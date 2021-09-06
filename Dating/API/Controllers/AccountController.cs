using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        // properties
        private readonly ITokenService tokenService;
        private readonly IMapper mapper;
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> singInManager;

        // constructor
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> singInManager, ITokenService tokenService, IMapper mapper)
        {
            this.singInManager = singInManager;
            this.userManager = userManager;
            this.tokenService = tokenService;
            this.mapper = mapper;
        }

        // methods
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.UserName)) return BadRequest("Username is taken by anyone else!");

            var user = this.mapper.Map<AppUser>(registerDto);

            user.UserName = registerDto.UserName.ToLower();

            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            var roleResult = await userManager.AddToRoleAsync(user, "Member");

            if (!roleResult.Succeeded) return BadRequest(result.Errors);

            return new UserDto
            {
                UserName = user.UserName,
                Token = await tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await this.userManager.Users.Include(p => p.Photos).SingleOrDefaultAsync(x => x.UserName == loginDto.UserName);

            if (user == null) return Unauthorized("Invalid Username!");

            var result = await singInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded) return Unauthorized();

            return new UserDto
            {
                UserName = user.UserName,
                Token = await tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        private async Task<bool> UserExists(string userName)
        {
            return await this.userManager.Users.AnyAsync(x => x.UserName == userName.ToLower());
        }
    }
}