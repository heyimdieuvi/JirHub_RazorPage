using JirHub.Entities.ViNTD.Models;
using JirHub.Repository.ViNTD.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JirHub.Repository.ViNTD.Repositories
{
    public class ProjectConfigRepository : GenericRepository<ProjectConfigsViNtd>
    {
        public ProjectConfigRepository() { }

        public ProjectConfigRepository(PRN222_SE1816_DbContext context) => _context = context;

        /// <summary>
        /// Gets the project configuration for a specific group.
        /// </summary>
        public async Task<ProjectConfigsViNtd> GetByGroupIdAsync(int groupId)
        {
            try
            {
                var projectConfig = await _context.ProjectConfigsViNtds
                    .FirstOrDefaultAsync(pc => pc.GroupId == groupId);
                return projectConfig;
            } catch (Exception ex)
            {
                return null;
            }

        }

        /// <summary>
        /// Creates or updates project configuration.
        /// </summary>
        public async Task<ProjectConfigsViNtd> UpsertAsync(ProjectConfigsViNtd config)
        {
            ProjectConfigsViNtd existing = null;
            if (config.GroupId.HasValue)
            {
                existing = await GetByGroupIdAsync(config.GroupId.Value);
            }

            if (existing != null)
            {
                // Update existing
                existing.JiraUrl = config.JiraUrl;
                existing.JiraEmail = config.JiraEmail;
                existing.JiraApiToken = config.JiraApiToken;
                existing.JiraProjectKey = config.JiraProjectKey;
                existing.GithubToken = config.GithubToken;

                _context.ProjectConfigsViNtds.Update(existing);
            }
            else
            {
                // Create new
                await _context.ProjectConfigsViNtds.AddAsync(config);
            }

            await _context.SaveChangesAsync();
            return existing ?? config;
        }
    }
}
