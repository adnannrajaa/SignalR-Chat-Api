﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SignalRChatApi.Helpers;
using SignalRChatApi.Model;
using SignalRChatApi.Services;

namespace SignalRChatApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private IUserService _userService;
        private readonly AppSettings _appSettings;

        public UserAccountController(ApplicationDbContext context, IUserService userService, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _userService = userService;
            _appSettings = appSettings.Value;
        }
        [HttpGet]
        public async Task<IActionResult> GetUserAccount()
        {
            var result = await _context.Users.Include(u => u.Connections).Include(u => u.Rooms).ToListAsync();
            if(result==null)
            {
                return NotFound( new { message = "No Record Found"});
            }
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> PostUserAccount([FromBody] UserDTO model)
        {
            User user = new User();
            user.user_name = model.user_name;
            user.password = await _userService.encryptPassword(model.password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] UserDTO  model)
        {
            if (ModelState.IsValid)
            {
                var user = _userService.Authenticate(model.user_name, model.password);
                if (user == null)
                    return Unauthorized(new { message = "Invalid_Credentials" });
                var tokenString = await GenerateJwtToken(user);
                LoggedInUser.UserId = user.id;
                LoggedInUser.UserName = user.user_name;
                // return basic user info and authentication token
                return Ok(new
                {
                    Id = user.id,
                    Username = user.user_name,
                    Token = tokenString
                });
            }
            else
            {
                return ValidationProblem("User name and password are required feilds");
            }
        }


        [NonAction]
        private async Task<object> GenerateJwtToken(User user)
        {

            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("user_id", user.id.ToString()),
                new Claim("user_name",user.user_name),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_appSettings.JwtExpireDays));

            var token = new JwtSecurityToken(
                _appSettings.JwtIssuer,
                _appSettings.JwtIssuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
