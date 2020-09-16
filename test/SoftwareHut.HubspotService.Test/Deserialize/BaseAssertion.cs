using AutoFixture.Idioms;
using AutoFixture.Xunit2;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Deserialize
{
    public abstract class BaseAssertion<T> where T : class
    {
        [Theory]
        [AutoData]
        public virtual void SutHasGuardClauses(GuardClauseAssertion guardClauseAssertion)
        {
            guardClauseAssertion.Verify(typeof(T));
        }

        [Theory]
        [AutoData]
        public virtual void SutCtorInitializesMembers(ConstructorInitializedMemberAssertion assertion)
        {
            assertion.Verify(typeof(T));
        }
    }
}