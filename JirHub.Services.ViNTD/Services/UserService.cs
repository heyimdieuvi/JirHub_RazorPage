using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;
using JirHub.Repository.ViNTD.Repositories;
using JirHub.Services.ViNTD.IServices;

namespace JirHub.Services.ViNTD.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository _repo;
        public UserService(UserRepository repo)
        {
            _repo = repo;
        }
        public async Task<List<User>> GetAllUser()
        {
            try
            {
                return await _repo.GetAllAsync();
            } catch (Exception ex)
            {
                return new List<User>();
            }
        }
    }
}
