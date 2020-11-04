using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Data;
using Shop.Services;
using Models;

namespace Shop.Controllers
{
    [Route("users")]
    public class UserController : Controller
    {
        [HttpGet]
        [Route("")]
        [Authorize(Roles = "manager")]
        public async Task<ActionResult<List<User>>> Get([FromServices] DataContext context)
        {
            var users = await context
                .Users
                .AsNoTracking()
                .ToListAsync();
            return users;
        }

        [HttpPost]
        [Route("")]
        [AllowAnonymous]
        //[Authorize(Roles = "manager")]
        public async Task<ActionResult<User>> Post(
            [FromServices]DataContext context,
            [FromBody]User model)
        {
            // verifica se os dados são válidos
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            
            try
            {
                // Força o usuário a ser sempre employee
                model.Role = "employee";

                context.Users.Add(model);
                await context.SaveChangesAsync();
                
                // Esconde a senha
                model.Password = "";
                return model;
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Não foi possível criar o usuário" });
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        [Authorize(Roles = "manager")]
        public async Task<ActionResult<User>> Put(
            [FromServices]DataContext context,
            int id,
            [FromBody] User model
        )
        {
            // Verifica se os dados são válidos
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            
            // Verifica se o ID informado é o mesmo do modelo
            if(id != model.Id)
                return NotFound(new { message = "Usuário não encontrado" });
            
            try
            {
                context.Entry(model).State = EntityState.Modified;
                await context.SaveChangesAsync();
                return model;
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Não foi possível criar o usuário" });
            }
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Authenticate(
            [FromServices]DataContext context,
            [FromBody]User model)
        {
            var user = await context.Users
                .AsNoTracking()
                .Where(x => x.Username == model.Username && x.Password == model.Password)
                .FirstOrDefaultAsync();
            
            if(user == null)
                return NotFound(new { message = "Usuário ou senha inválidos" });

            var token = TokenService.GenerateToken(user);

            //Esconde a senha
            model.Password = "";
            
            return new
            {
                user = user,
                token = token
            };
        }
    }
}