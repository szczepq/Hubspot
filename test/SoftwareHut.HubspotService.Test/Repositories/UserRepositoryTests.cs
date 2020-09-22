using FakeItEasy;
using FluentAssertions;
using MockQueryable.FakeItEasy;
using SoftwareHut.HubspotService.DbModels;
using SoftwareHut.HubspotService.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SoftwareHut.HubspotService.Test.Attributes;
using SoftwareHut.HubspotService.Test.Deserialize;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Repositories
{
    [IntegrationTests]
    public class UserRepositoryTests: BaseAssertion<UserRepository>
    {
        [Theory, AutoFakeData]
        public async Task CreateUser_Ok(UserRepository sut, int externalId, string email)
        {
            var contacts = new List<HubspotDbContact>();
            var mock = contacts.AsQueryable().BuildMockDbSet();
            A.CallTo(() => mock.AddAsync(A<HubspotDbContact>._, A<CancellationToken>._))
                .ReturnsLazily(call =>
                {
                    contacts.Add((HubspotDbContact) call.Arguments[0]);
                    return default;
                });
            A.CallTo(() => sut.HubspotDbContext.Users)
                .Returns(mock);

            await sut.CreateUserAsync(externalId, email);
            var entity = mock.Single();

            Assert.Equal(externalId, entity.ExternalId);
            Assert.Equal(email, entity.Email);
        }

        [Theory, AutoFakeData]
        public async Task GetAllUsers_Ok(UserRepository sut, List<HubspotDbContact> contacts)
        {
            var mock = contacts.AsQueryable().BuildMockDbSet();
            A.CallTo(() => sut.HubspotDbContext.Users)
                .Returns(mock);

            var users = await sut.GetAllUsersAsync();

            Assert.NotNull(users);
            users.Should().BeEquivalentTo(contacts);
        }
    }
}