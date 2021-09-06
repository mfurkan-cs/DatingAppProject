using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await this.context.Users
                .Include(p => p.Photos)
                .ToListAsync();
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await this.context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string userName)
        {
            return await this.context.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == userName);
        }

        public void Update(AppUser user)
        {
            this.context.Entry(user).State = EntityState.Modified;
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var query = this.context.Users.AsQueryable();

            query = query.Where(u => u.UserName != userParams.CurrentUserName);
            query = query.Where(u => u.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            };

            return await PagedList<MemberDto>.CreateAsync(query.ProjectTo<MemberDto>(mapper.ConfigurationProvider).AsNoTracking(), userParams.PageNumber, userParams.PageSize);
        }

        public async Task<MemberDto> GetMemberAsync(string userName, bool isCurrentUser)
        {
            var query = context.Users
                .Where(x => x.UserName == userName)
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
                .AsQueryable();

            if (isCurrentUser) query = query.IgnoreQueryFilters();
            
            return await query.FirstOrDefaultAsync();
        }

        public async Task<string> GetUserGender(string userName)
        {
            return await context.Users
                .Where(x => x.UserName == userName)
                .Select(x => x.Gender).FirstOrDefaultAsync();
        }

        public async Task<AppUser> GetUserByPhotoId(int photoId)
        {
            return await context.Users
                .Include(p => p.Photos)
                .IgnoreQueryFilters()
                .Where(p => p.Photos.Any(p => p.Id == photoId))
                .FirstOrDefaultAsync();
        }
    }
}