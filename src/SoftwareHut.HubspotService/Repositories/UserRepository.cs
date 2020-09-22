using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SoftwareHut.HubspotService.DbModels;

namespace SoftwareHut.HubspotService.Repositories
{
    public interface IUserRepository
    {
        Task<int> CreateUserAsync(int externalId, string email);
        Task<List<HubspotDbContact>> GetAllUsersAsync();
    }

    public class UserRepository : IUserRepository
    {
        public IHubspotDbContext HubspotDbContext { get; }

        public UserRepository(IHubspotDbContext hubspotDbContext)
        {
            HubspotDbContext = hubspotDbContext ?? throw new ArgumentNullException(nameof(hubspotDbContext));
        }

        public Task<int> CreateUserAsync(int externalId, string email)
        {
            if(string.IsNullOrWhiteSpace(email)) 
                throw new ArgumentNullException(nameof(email));

            return CreateUserInternalAsync(externalId, email);
        }

        private async Task<int> CreateUserInternalAsync(int externalId, string email)
        {
            var user = new HubspotDbContact
            {
                ExternalId = externalId,
                Email = email
            };

            await HubspotDbContext.Users.AddAsync(user);
            await HubspotDbContext.SaveChangesAsync();

            return user.Id;
        }

        public Task<List<HubspotDbContact>> GetAllUsersAsync() => HubspotDbContext.Users.ToListAsync();
    }
}