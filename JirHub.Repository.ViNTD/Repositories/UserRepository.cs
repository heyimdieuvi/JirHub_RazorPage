using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;
using JirHub.Repository.ViNTD.Base;
using Microsoft.EntityFrameworkCore;

namespace JirHub.Repository.ViNTD.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository() { }

        public async Task<User> GetUserAccountAsync(string username, string password)
        {
            return await _context.Users
                .Include(u => u.GroupMembers)
                .FirstOrDefaultAsync(u => u.Email == username && u.PasswordHash == password);
        }
        
    }
}
