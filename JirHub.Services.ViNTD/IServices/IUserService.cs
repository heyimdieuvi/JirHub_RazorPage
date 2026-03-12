using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Entities.ViNTD.Models;

namespace JirHub.Services.ViNTD.IServices
{
    public interface IUserService
    {
        Task<List<User>> GetAllUser();
    }
}
