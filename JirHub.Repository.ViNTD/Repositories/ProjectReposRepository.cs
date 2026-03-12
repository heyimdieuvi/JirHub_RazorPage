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
    public class ProjectReposRepository : GenericRepository<ProjectReposViNtd>
    {
        public ProjectReposRepository() { }

        public ProjectReposRepository(PRN222_SE1816_DbContext context) => _context = context;

        /// <summary>
        /// Gets all active repositories for a specific group.
        /// </summary>
        public async Task<List<ProjectReposViNtd>> GetByGroupIdAsync(int groupId)
        {
            return await _context.ProjectReposViNtds
                .Where(pr => pr.GroupId == groupId)
                .ToListAsync();
        }

        /// <summary>
        /// Creates or updates a repository record.
        /// </summary>
        public async Task<ProjectReposViNtd> UpsertAsync(ProjectReposViNtd repo)
        {
            var existing = await _context.ProjectReposViNtds
                .FirstOrDefaultAsync(pr => pr.GroupId == repo.GroupId && pr.RepoUrl == repo.RepoUrl);

            if (existing != null)
            {
                // Update existing
                existing.RepoName = repo.RepoName;
                existing.IsActive = repo.IsActive;

                _context.ProjectReposViNtds.Update(existing);
            }
            else
            {
                // Create new
                await _context.ProjectReposViNtds.AddAsync(repo);
            }

            await _context.SaveChangesAsync();
            return existing ?? repo;
        }

        /// <summary>
        /// Deactivates all repositories for a group (used before adding new one).
        /// </summary>
        public async Task DeactivateAllForGroupAsync(int groupId)
        {
            var repos = await GetByGroupIdAsync(groupId);
            foreach (var repo in repos)
            {
                repo.IsActive = false;
            }
            await _context.SaveChangesAsync();
        }
    }
}
