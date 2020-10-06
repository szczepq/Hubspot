# Testing  guideline for .net developer

In this article I will show you how to:
* create a health test
* test a configuration
* group tests by categories
* test serialization and deserialization
* automatically fake data for object properties 
* have an influence on how data is faked for an object
* verifies that a method or constructor has appropriate Guard and if the member (property or field) is correctly initialized
* mock DbSet and responses from the database
* test retry and cache policies
* specify a set of data for the faked object
* test validators
* mock responses with external API
* write an integration tests


## Health test
*switch to tag `stage1-health` to have a complete solution for this stage*

Create a solution and add 2 projects:
* ASP.NET Core Web Application with just API
* xUnit Test project

The first thing I usually do in my projects is to add a `health controller` and a `health test` so let's begin with Controller. 
Go to `Startup.cs` and use a build in extension method for `IServiceCollection` in `ConfigureServices` method.
```
services.AddHealthChecks();
```
To complete this task you must specify an address for the new endpoint. 
You have to use and an extension method for `IApplicationBuilder` in `Configure` method.
```
app.UseHealthChecks("/health");
```
This configuration is exposing an endpoint for health probe that can be used by `Azure` or `kubernetes` to check if your application is still working.
You can run your app and navigate to `/health` uri or we can add the first test for that.

Check if you have all necessary nugets to complete this task:
* Microsoft.AspNetCore.Mvc.Testing
* Microsoft.NET.Test.Sdk
* xunit
* xunit.runner.visualstudio
* coverlet.collector

Create an `IntegrationTests` and `HealthTests` folder inside, add references to our main project. 
We have to create three files:
* BootstrappedTestFixture - class with per-test-collection fixture data
* BootstrappedTestCollection - used to decorate xUnit.net test classes and collections to indicate a test which has per-test-collection fixture data
* HealthTest - out first integration test

```csharp
public class BootstrappedTestFixture : WebApplicationFactory<Startup>, IAsyncLifetime
{
    public HttpClient TestClient { get; }

    public BootstrappedTestFixture()
    {
        TestClient = CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;
}
```
```csharp
[CollectionDefinition(CollectionName)]
public class BootstrappedTestCollection : ICollectionFixture<BootstrappedTestFixture>
{
    public const string CollectionName = "Bootstrapped tests";
}
```
```csharp
[Collection(BootstrappedTestCollection.CollectionName)]
public class HealthTest
{
    public BootstrappedTestFixture BootstrappedTestFixture { get; }

    public HealthTest(BootstrappedTestFixture bootstrappedTestFixture)
    {
        BootstrappedTestFixture = bootstrappedTestFixture ??
                                    throw new ArgumentNullException(nameof(bootstrappedTestFixture));
    }

    [Fact]
    public async Task Get_Health_ShouldReturn200Ok()
    {
        var response = await BootstrappedTestFixture.TestClient.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```
Ok, but what is happening here? 
* We created a `TestClient` to be able to call and endpoint from out application because our `BootstrappedTestFixture`
 inherits  from generic class `WebApplicationFactory` that uses the `Startup` class from our main project.
* We created an attribute that allows xUnit to inject the `TestClient` to our test class
* We decorated our test so we can send a `Get` request to `health` endpoint and check if a response status if `200`.

You can run this test and check if our application is working!


## Verifying a configuration
*switch to tag `stage2-configuration` to have a complete solution of this stage*

As we are going to integrate with Hubspot and get some contacts information via their [API](https://legacydocs.hubspot.com/docs/methods/contacts/get_contacts)
we can have `base url` and `hapikey` specified in the configuration. Lets add those two values to `appsettings.json`
```json
"Hubspot": {
    "hapikey": "demo",
    "baseUrl": "https://api.hubapi.com"
} 
```
and add a class with an interface to map configuration options to
```csharp
public interface IHubspotConfiguration
{
    string HapiKey { get; }
    string BaseUrl { get; }

}
public class HubspotConfiguration: IHubspotConfiguration
{
    public const string SectionName = "Hubspot";

    public string HapiKey { get; set; }
    public string BaseUrl { get; set; }
}
```
Now we can get the data from the configuration provider and register it in dependency container 
but the point of this step if to check if the configuration that is required is not missing.
To do that we can use `IStartupFilter` that will try to get our configuration during startup and throws an 
`OptionsValidationException` exception if it's missing.
```csharp
public class ConfigurationValidationStartupFilter<TConfigurationClass> : IStartupFilter
    where TConfigurationClass : class
{
    public IServiceProvider ServiceProvider { get; }

    public ConfigurationValidationStartupFilter(
        IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public Action<IApplicationBuilder> Configure(
        Action<IApplicationBuilder> next)
    {
        try
        {
            ServiceProvider.GetService(typeof(TConfigurationClass));
        }
        catch (OptionsValidationException ex)
        {
            throw;
        }
        return next;
    }
}
```

Additionally, we can add `[Required]` attribute to all properties of `HubspotConfiguration` class and use `ValidateDataAnnotations` extension method of an `OptionsBuilder` class
during registration in the container. A good and reusable place for everything needed to add configuration to DI
is an extension method for `IServiceCollection`.

```csharp
public static IServiceCollection AddConfiguration<TConfigurationInterface, TConfigurationClass>(
    this IServiceCollection serviceCollection,
    IConfiguration configuration)
    where TConfigurationInterface : class
    where TConfigurationClass : class, TConfigurationInterface, new()
{
    serviceCollection.Configure<TConfigurationClass>(configuration);
    OptionsBuilder<TConfigurationClass> optionsBuilder = serviceCollection.AddOptions<TConfigurationClass>();
    optionsBuilder.ValidateDataAnnotations();
            
    serviceCollection.AddTransient(sp => sp.GetRequiredService<IOptions<TConfigurationClass>>().Value);
    serviceCollection.AddTransient<IStartupFilter, ConfigurationValidationStartupFilter<TConfigurationClass>>();
    serviceCollection.AddTransient(sp => (TConfigurationInterface)sp.GetRequiredService<TConfigurationClass>());
            
    return serviceCollection;
}
```

And now we can use it

```csharp
    services.AddConfiguration<IHubspotConfiguration, HubspotConfiguration>(
        Configuration.GetSection(HubspotConfiguration.SectionName));
```

When you run an application and some of the properties or the whole section will be missing you will be notified by en exception.

But now you may ask: Where is the test for that? Do you remember our [first test](#Health-test) that is his hidden value?
Please remove one of the values from `Hubspot` section in `appsettings.json` and run this test. It will fail.

## Grupping tests
*switch to tag `stage3-grouping` to have a complete solution of this stage*

As we are going to add more tests with different types (unit tests, integration tests, convention tests) we can make our life easier
by creating attributes for all of those to mark tests. Those attributes are used by Visual Studio in Test Explorer or could be used
during pull requests verification for example: 
* allow PR to be merged to `development` branch only if all unit tests are passed
* allow PR to be merged to `master` branch if all tests are passing

So we have fast verification when we don't have to wait a lot on each PR and full verification when even a database is required.

To do that we need to get familiar with `ITraitAttribute` and `ITraitDiscoverer`. The first one allows us to create such an attribute to mark tests
and the second one is needed during the build to discover our attributes.

```csharp
[TraitDiscoverer("SoftwareHut.HubspotService.Test.Attributes.IntegrationTestsDiscoverer",
    "SoftwareHut.HubspotService.Test")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class IntegrationTestsAttribute : Attribute, ITraitAttribute
{
    public IntegrationTestsAttribute() { }
}
public class IntegrationTestsDiscoverer : ITraitDiscoverer
{
    public const string KEY = "Category";
    public const string Category = "IntegrationTests";

    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>(KEY, Category);
    }
}
```

To connect `TraitDiscoverer` with a `TraitAttribute` you need to pass a type-name and assembly-name to `TraitDiscoverer` attribute that is used
on `UnitTestsAttribute`. The name of a category that you use in `IntegrationTestsDiscoverer` will be displayed
in Test Explorer. Add this attribute to out HealthTest, rebuild the project and check `Travis` column in Test Explorer.
You can not create two more for unit and coverage tests.


## Http client
*switch to tag `stage4-serialization` to have a complete solution of this stage*

The build-in HttpClient is a very unpleasant guy to test. This class is sealed and doesn't implement any interface that we could mock so I'm using `Refit` which wraps `HttpClient` and is also a very nice timesaver. Find and install `Refit.HttpClientFactory` nuget package and create `IHubspotClient` interface in `Clients` folder:

```csharp
public interface IHubspotClient
{
    [Get("/contacts/v1/lists/all/contacts/all?hapikey={hapikey}&count={count}")]
    Task<HubspotContacts> GetContacts(
        string hapikey, 
        int count);
}
```

We also have to create a hubspot response model (I'm only interested in 2 properties: `id` and `e-mail`):
```csharp
public class HubspotContacts
{
    [JsonProperty("contacts")] 
    public List<HubspotContact> HubspotContact { get; private set; }

    [JsonConstructor]
    private HubspotContacts() { }

    public HubspotContacts(List<HubspotContact> hubspotContact)
    {
        HubspotContact = hubspotContact ?? throw new ArgumentNullException(nameof(hubspotContact));
    }
}

public class HubspotContact
{
    [JsonProperty("vid")] 
    public int Id { get; private set; }

    [JsonProperty("identity-profiles")] 
    public List<HubspotProfile> Profiles { get; private set; }

    [JsonConstructor]
    private HubspotContact() { }

    public HubspotContact(int id, List<HubspotProfile> profiles)
    {
        Id = id;
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    }
}

public class HubspotProfile
{
    [JsonProperty("identities")] 
    public List<HubspotIdentity> Identity { get; private set; }

    [JsonConstructor]
    private HubspotProfile() { }

    public HubspotProfile(List<HubspotIdentity> identity)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }
}

public class HubspotIdentity
{
    [JsonProperty("type")] 
    public string Type { get; private set; }

    [JsonProperty("value")] 
    public string Value { get; private set; }

    [JsonConstructor]
    private HubspotIdentity() { }

    public HubspotIdentity(string type, string value)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
```

To configure `Refit` we need to use `AddRefitClient` extension methos and pass the `BaseAddress` from `HubspotConfiguration` doring registering `IHubspotClient` in dependency container.

```csharp
var hubspotConfiguration =
    Configuration.GetSection(HubspotConfiguration.SectionName).Get<HubspotConfiguration>();
services.AddRefitClient<IHubspotClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(hubspotConfiguration.BaseUrl));
```

### Testing deserialization

To check if our definition of model fits the response we have to add a test to check deserialization of JSON response example.
* go to test project and install 2 nuget packages:
  * `Newtonsoft.Json`
  * `FluentAssertions`
* add `HubspotContactsTests` class in `Deserialize` folder
* create a test (I'll cut out all not necessary properties to make it more readable)

```csharp
        [Fact]
        public void HubspotContacts_ShouldBeDeserialize()
        {
            var sampleJson = @"
            {
              ""contacts"": [
                {
                  ""vid"": 204727,
                  ""identity-profiles"": [
                    {
                      ""identities"": [
                        {
                          ""type"": ""EMAIL"",
                          ""value"": ""mgnew-email@hubspot.com""
                        }]}]},
                {
                  ""vid"": 207303,
                  ""identity-profiles"": [
                    {
                      ""identities"": [
                        {
                          ""type"": ""EMAIL"",
                          ""value"": ""email_0be34aebe5@abctest.com"",
                        }]}]}
                ]}
            ";

            var expected = new HubspotContacts(
                new List<HubspotContact>
                {
                    new HubspotContact(
                        204727,
                        new List<HubspotProfile>
                        {
                            new HubspotProfile(
                                new List<HubspotIdentity>
                                {
                                    new HubspotIdentity("EMAIL", "mgnew-email@hubspot.com")
                                })
                        }),
                    new HubspotContact(
                        207303,
                        new List<HubspotProfile>
                        {
                            new HubspotProfile(
                                new List<HubspotIdentity>
                                {
                                    new HubspotIdentity("EMAIL", "email_0be34aebe5@abctest.com")
                                })
                        })
                }
            );


            var response = JsonConvert.DeserializeObject<HubspotContacts>(sampleJson);
            Assert.NotNull(response);
            response.Should().BeEquivalentTo(expected);
        }
```
We used `Newtonsoft.Json` to deserialize a JSON example as it will be also used by Refit in our application and we used `FluentAssertions` to compare whole objects instead of all properties one by one.

### Serialization

We are also going to send some data to hubspot so we should test if our object is serialized as it's expected by hubspot.

Add create contact endpoint to `IHubspotClient`

```csharp
        [Post("/contacts/v1/contact?hapikey={hapikey}")]
        Task<HubspotProfile> CreateContactsAsync(
            string hapikey,
            CreateHubspotContact contacts);
```

Add `CreateHubspotContact` class

```csharp
    public class CreateHubspotContact
    {
        [JsonProperty("properties")]
        public List<CreateContactProperty> Properties { get; }

        public CreateHubspotContact(List<CreateContactProperty> properties)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }
    }

    public class CreateContactProperty
    {
        [JsonProperty("property")]
        public string Property { get; }

        [JsonProperty("value")]
        public string Value { get;  }

        public CreateContactProperty(string property, string value)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
```
### Testing Serialization
Install `Quibble.Xunit` nuget packages to compare JSON strings and add `CreateHubspotContactTests` class with test

 ```csharp
        [Fact]
        public void CreateHubspotContact_ShouldBeSerialized()
        {
            var expected = @"
            {
              ""properties"": [
                        {
                            ""property"": ""email"",
                            ""value"": ""testingapis@hubspot.com""
                        },
                        {
                            ""property"": ""firstname"",
                            ""value"": ""Adrian""
                        }
                    ]
                }
            ";
            var newContact = new CreateHubspotContact(
                new List<CreateContactProperty>
                {
                    new CreateContactProperty("email", "testingapis@hubspot.com"),
                    new CreateContactProperty("firstname", "Adrian"),
                });
            var json = JsonConvert.SerializeObject(newContact);

            JsonAssert.Equal(expected, json);
        }
 ```

 ## Mapper and faking data
 *switch to tag `stage5-mapper-and-fake` to have a complete solution of this stage*

For fetching contacts from hubspot our API will return a different schema then we are receiving from hubspot. The same situation will be for creating a contact in hubspot.
We have to add two new models to our project

```csharp
    public class ContactsList
    {
        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; }

        public ContactsList(List<Contact> contacts)
        {
            Contacts = contacts ?? throw new ArgumentNullException(nameof(contacts));
        }
    }

    public class Contact
    {
        [JsonProperty("externalId")]
        public int ExternalId { get; }

        [JsonProperty("email")]
        public string Email { get;  }

        public Contact(int externalId, string email)
        {
            ExternalId = externalId;
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }
    }
```
and
```csharp
    public class CreateContact
    {
        [JsonProperty("firstName")]
        public string FirstName { get; private set; }

        [JsonProperty("email")] 
        public string Email { get; private set; }

        [JsonConstructor]
        private CreateContact() { }

        public CreateContact(string firstName, string email)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }
    }
```
 and create a mapper that will change one type to another.
 ```csharp
    public interface IHubspotMapper
    {
        ContactsList FromHubspotContacts(HubspotContacts hubspotContacts);
        Contact FromHubspotContact(HubspotContact hubspotContact);
        CreateHubspotContact ToCreateHubspotContact(CreateContact contact);
    }

    public class HubspotMapper : IHubspotMapper
    {
        public ContactsList FromHubspotContacts(HubspotContacts hubspotContacts)
        {
            if (hubspotContacts == null) throw new ArgumentNullException(nameof(hubspotContacts));

            return new ContactsList(
                hubspotContacts.HubspotContact
                    .Select(FromHubspotContact)
                    .ToList());
        }

        public Contact FromHubspotContact(HubspotContact hubspotContact)
        {
            return new Contact(
                hubspotContact.Id,
                hubspotContact.Profiles.First()
                    .Identity.FirstOrDefault(y => y.Type == "EMAIL")?.Value);
        }

        public CreateHubspotContact ToCreateHubspotContact(CreateContact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            return new CreateHubspotContact(new List<CreateContactProperty>
            {
                new CreateContactProperty("email", contact.Email),
                new CreateContactProperty("firstname", contact.FirstName)
            });
        }
    }
 ```
 Add mapper to DI

 ```csharp
services.AddSingleton<IHubspotMapper, HubspotMapper>();
 ```

 #### Testing mapper
 Now we need to write some tests for the mapper. To do that we will have to put some values in `HubspotContacts` and `CreateContact` or make our live 
 easier by using `AutoDataAttribute` from  `AutoFixture.Xunit2` with `Theory` instead of `Fact`.
 
 Create a test class `HubspotMapperTests` with first test for `ToCreateHubspotContact` method:

 ```csharp
    [Theory, AutoData]
    public void ToCreateHubspotContact_Ok(
        HubspotMapper sut,
        CreateContact contact)
    {
        var createHubspotContact = sut.ToCreateHubspotContact(contact);

        Assert.NotNull(createHubspotContact);
        Assert.Equal(2, createHubspotContact.Properties.Count);
        
        var email = createHubspotContact.Properties
            .FirstOrDefault(x => x.Property == "email");
        Assert.NotNull(email);
        Assert.Equal(contact.Email, email.Value);

        var firstName = createHubspotContact.Properties
            .FirstOrDefault(x => x.Property == "firstname");
        Assert.NotNull(firstName);
        Assert.Equal(contact.FirstName, firstName.Value);
    }
 ```
 That is everything that we need for this test case, our `CreateContact` object will be filled out with random values by `AutoDataAttribute`.
 But we can't do the same for `FromHubspotContact` method because we expect an "EMAIL" value in `Type` property in `HubspotIdentity` object.
To customize the way of `AutoDataAttribute` is faking values we have to create our own Attribute. 
Our `AutoFakeDataAttribute` have to inherite from `AutoDataAttribute`. 
We have also to create a `CompositeCustomization` that will combine a default `AutoFakeItEasyCustomization` which is used by `AutoFakeItEasyCustomization`
and a new one with our customization. Our customization has to replace the value passed via `type` parameter of `HubspotIdentity` constructor
so we have to create a `SpecimenBuilder` for a constructor to fill the data. Besides that, we have to check if there is a parameter in a constructor with a given name.
We will start from the end.

* create a helper method for checking is given parameter exists in the constructor
  
```csharp
    public static class ReflectionHelper
    {
        public static bool IsParameterDeclaredInCtor<TTarget, TParameterType>(this object target, string paramName)
        {
            return target is ParameterInfo parameter &&
                   parameter.Member.DeclaringType == typeof(TTarget) &&
                   parameter.Member.MemberType == MemberTypes.Constructor &&
                   parameter.ParameterType == typeof(TParameterType) &&
                   parameter.Name == paramName;
        }
    }
```
* create a `SpecimenBuilder` for a constructor
```csharp
    public class ConstructorArgumentSpecimen<TTarget, TParameterType> : ISpecimenBuilder
    {
        private readonly string _paramName;
        private readonly Func<TParameterType> _valueFactory;

        public ConstructorArgumentSpecimen(string paramName, Func<TParameterType> valueFactory)
        {
            _paramName = paramName;
            _valueFactory = valueFactory;
        }

        public object Create(object request, ISpecimenContext context) =>
            request.IsParameterDeclaredInCtor<TTarget, TParameterType>(_paramName)
                ? (object) _valueFactory.Invoke()
                : new NoSpecimen();
    }
```
* create customization
```csharp
    public class TestCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(
                new ConstructorArgumentSpecimen<HubspotIdentity, string>(
                    "type", ()=> "EMAIL"));
        }
    }
```

Add `AutoFixture.AutoFakeItEasy` nuget because we will need it for creating `CompositeCustomization`

```csharp
    public class AutoFakeCustomization : CompositeCustomization
    {
        public AutoFakeCustomization()
            : base(
                new AutoFakeItEasyCustomization { GenerateDelegates = true },
                new TestCustomization())
        {
        }
    }
```
And finally add `AutoFakeDataAttribute`
```csharp
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AutoFakeDataAttribute : AutoDataAttribute
    {
        public AutoFakeDataAttribute()
            : base(() => new Fixture().Customize(new AutoFakeCustomization()))
        {
        }
    }
```
Now we can create a test for `FromHubspotContact` method
```csharp
    [Theory, AutoFakeData]
    public void FromHubspotContact_Ok(
        HubspotMapper sut,
        HubspotContact hubspotContact)
    {
        var contact = sut.FromHubspotContact(hubspotContact);
        var email = hubspotContact.Profiles.First().Identity.First(x => x.Type == "EMAIL").Value;

        Assert.NotNull(contact);
        Assert.Equal(hubspotContact.Id, contact.ExternalId);
        Assert.Equal(email, contact.Email);
    }
```

and for `FromHubspotContacts` method

```csharp
    [Theory, AutoFakeData]
    public void FromHubspotContacts_Ok(
        HubspotMapper sut,
        HubspotContacts hubspotContacts)
    {
        var contactsList = sut.FromHubspotContacts(hubspotContacts);

        Assert.NotNull(contactsList);
        Assert.Equal(hubspotContacts.HubspotContact.Count, contactsList.Contacts.Count);
        Assert.True(contactsList.Contacts.All(x => x != null));
    }
```

If you want an `email` value to look like a real email instead of a random string you can use `Faker.Net` nuget package and add customization for `value` property.

```csharp
    fixture.Customizations.Add(
        new ConstructorArgumentSpecimen<HubspotIdentity, string>(
            "value", Faker.Internet.Email));
```

If you need to customize values by properties or fields you have to create equivalent classes to `ConstructorArgumentSpecimen`.

### Base assertions
A good idea is to create a base class for your tests that will contain two tests that will:
* verifies that a method or constructor has appropriate Guard
* verifies that a member (property or field) is correctly initialized

To do that we have to add next nuget package `AutoFixture.Idioms` and create a base class
```csharp
    public abstract class BaseAssertion<T> where T : class
    {
        [Theory]
        [AutoFakeData]
        public virtual void SutHasGuardClauses(GuardClauseAssertion guardClauseAssertion)
        {
            guardClauseAssertion.Verify(typeof(T));
        }

        [Theory]
        [AutoFakeData]
        public virtual void SutCtorInitializesMembers(ConstructorInitializedMemberAssertion assertion)
        {
            assertion.Verify(typeof(T));
        }
    }
```

We can add this tests for all our model classes
```csharp
    public class ContactsListTests : BaseAssertion<ContactsList> { }
    public class CreateContactTests : BaseAssertion<CreateContact> { }
    public class CreateHubspotContactTests : BaseAssertion<CreateHubspotContact> { }
    public class HubspotContactsTests : BaseAssertion<HubspotContacts> { }
```
and add it to `HubspotMapperTests`
```csharp
    public class HubspotMapperTests: BaseAssertion<HubspotMapper>
    ...
```
Now we won't ever forget to check if all parameters passed to a constructor and public methods contain null-checks and all parameters from constructors have their owns properties or fields with the same names.

## Entity framework
 *switch to tag `stage6-EF` to have a complete solution of this stage*

We are going to create a simple database with a single table of users. We also need to create a repository with two methods for inserting and retrieving data.
We will use Entity Framework for that purpose.
Add two nuget packages:
* Microsoft.EntityFrameworkCore.SqlServer
* Microsoft.EntityFrameworkCore.Tools

Add ConnectionStrings section to `appsettings.json` file
```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Initial Catalog=hubspot;Integrated Security=true;Persist Security Info=True;"
  } 
```

Add `HubspotDbContact` model class
```csharp
    public class HubspotDbContact
    {
        public int Id { get; set; }
        public int ExternalId { get; set; }
        public string Email { get; set; }
    }
```

Create `DbContext` class
```csharp
    public interface IHubspotDbContext
    {
        DbSet<HubspotDbContact> Users { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    }

    public class HubspotDbContext : DbContext, IHubspotDbContext
    {
        public HubspotDbContext(DbContextOptions<HubspotDbContext> options) : base(options) { }
        public DbSet<HubspotDbContact> Users { get; set; }
    }
```
Add `hubspot` database in your management studio. 
Use `Add-Migration` and `Update-Database` command in `Package Manager Console` to create migration and create tables in database.

Add `Repository` class
```csharp
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
```
The `CreateUser` method is split to `CreateUserAsync` and `CreateUserInternalAsync` because our GuardClause test is failing for `public async` method.
The Exception that you can expect will be: 
```
    AutoFixture.Idioms.GuardClauseException : A Guard Clause test was performed on a method that returns a Task, Task<T> (possibly in an 'async' method), but the test failed. See the inner exception for more details. However, because of the async nature of the task, this test failure may look like a false positive. Perhaps you already have a Guard Clause in place, but inside the Task or inside a method marked with the 'async' keyword (if you're using C#); if this is the case, the Guard Clause is dormant, and will first be triggered when a client accesses the Result of the Task. This doesn't adhere to the Fail Fast principle, so should be addressed.
```
So the walkaround for this is to leade all null-checks in a public method that does not contain `async` keyword and move everything that needs to `await` to `private async` method.

We have everything in place so please register it to DI container.

```csharp
    // Repository
    services.AddTransient<IUserRepository, UserRepository>();

    // SQL
    services.AddDbContext<HubspotDbContext>(options =>
        options.UseSqlServer(
            Configuration.GetConnectionString("DefaultConnection")));
    services.AddTransient<IHubspotDbContext, HubspotDbContext>();
```

#### Testing Repository and mocking DbSets

To make our life easier we need `MockQueryable.FakeItEasy` nuget packages in our test project. 
This package provides an ability to mock `DbSet` class with `Queryable` results.
Please add `UserRepositoryTests` test class

```csharp
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
```
In the first test we are creating an empty listy od Users, mock a `DbSet` to return this list as a normal `Queryable` response and mock what is going to happen when someone will use `AddAsync` method that is called by `CreateUserAsync` method in a repository - add a new User to our list. And then we can check if our new user contains `ExternalId` and `Email` properties with values that we used.

In the second test we are faking a List or users, build mock DbSet as before and check if all users are returned by `GetAllUsersAsync` method without any modifications.

## Retry and cache policy
 *switch to tag `stage7-policy` to have a complete solution of this stage*

The nature of integrating with an external APIs like hubspot is that sometimes a connection is getting lost, we receive timeout or hit too many request limitation.
To help with that we need to create a retry and cache policy. Polly is a great nuget package that will help us.
Let's add `Polly.Caching.Memory` nuget package.

### Cache policy
At first we need to add configuration
```csharp
    public interface ICachePolicyConfiguration
    {
        TimeSpan CacheDuration { get; }
    }

    public class CachePolicyConfiguration : ICachePolicyConfiguration
    {
        public const string SectionName = "CachePolicy";
        public int? CacheDurationSec { get; set; }

        public TimeSpan CacheDuration =>
            TimeSpan.FromSeconds(CacheDurationSec ?? 10);
    }
```

All necessery values has their default values that can be override in `appsettings.json` file.
Add them to DI container.

```csharp
    services.AddConfiguration<ICachePolicyConfiguration, CachePolicyConfiguration>(
        Configuration.GetSection(CachePolicyConfiguration.SectionName));           
```

Cache policy has to be created independently for each of type that we wont to cache so we will create a base class for it

```csharp
    public interface ICachePolicy<T>
    {
        Task<T> ExecuteAsync(Func<Context, Task<T>> action, Context context);
    }

    public abstract class CachePolicy<T> : ICachePolicy<T>
    {
        public IAsyncCacheProvider AsyncCacheProvider { get; }
        private readonly AsyncCachePolicy<T> _policyInternal;

        public CachePolicy(IAsyncCacheProvider asyncCacheProvider, TimeSpan validity)
        {
            AsyncCacheProvider = asyncCacheProvider ?? throw new ArgumentNullException(nameof(asyncCacheProvider));

            _policyInternal = Policy.CacheAsync<T>(asyncCacheProvider, validity);
        }

        public Task<T> ExecuteAsync(Func<Context, Task<T>> action, Context context)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (context == null) throw new ArgumentNullException(nameof(context));

            return _policyInternal.ExecuteAsync(action, context);
        }
    }
```

and the one for `HubspotContacts`

```csharp
    public interface IHubspotContactsCachePolicy : ICachePolicy<HubspotContacts>
    {
    }
    public class HubspotContactsCachePolicy : CachePolicy<HubspotContacts>,
        IHubspotContactsCachePolicy
    {
        public ICachePolicyConfiguration CachePolicyConfiguration { get; }

        public HubspotContactsCachePolicy(
            IAsyncCacheProvider asyncCacheProvider,
            ICachePolicyConfiguration cachePolicyConfiguration)
            : base(
                asyncCacheProvider ?? throw new ArgumentNullException(nameof(asyncCacheProvider)),
                cachePolicyConfiguration?.CacheDuration ?? throw new ArgumentNullException(nameof(cachePolicyConfiguration)))
        {
            CachePolicyConfiguration = cachePolicyConfiguration;
        }
    }
```

we need to add everything to DI container

```csharp
    services.AddMemoryCache();
    services.AddSingleton<IAsyncCacheProvider, MemoryCacheProvider>();
    services.AddSingleton<IHubspotContactsCachePolicy, HubspotContactsCachePolicy>();
```

### Testing cache policy
To be able to perform tests on our cache policy we will have to extend our `BootstrappedTestFixture` by a method for getting instances from DI container because we will need to get `IAsyncCacheProvider`.

```csharp
    public T GetService<T>() => Services.GetRequiredService<T>();
```

Let's create integration tests for our cache policy.

```csharp
 [Collection(BootstrappedTestCollection.CollectionName)]
    public class HubspotContactsCachePolicyTests : BaseAssertion<HubspotContactsCachePolicy>
    {
        public BootstrappedTestFixture BootstrappedTestFixture { get; }

        public HubspotContactsCachePolicyTests(BootstrappedTestFixture bootstrappedTestFixture)
        {
            BootstrappedTestFixture = bootstrappedTestFixture;
        }

        [Theory, AutoFakeData]
        public async Task ExecuteAsync_PositiveTtl(
            int count,
            CachePolicyConfiguration cachingConfig,
            HubspotContacts hubspotContactsFirst,
            HubspotContacts hubspotContactsSecond,
            Func<int, Task<HubspotContacts>> func)
        {
            const int ttl = 1;
            cachingConfig.CacheDurationSec = ttl;
            var asyncCacheProvider = BootstrappedTestFixture.GetService<IAsyncCacheProvider>();
            A.CallTo(() => func(count))
                .Returns(hubspotContactsFirst).Once()
                .Then.Returns(hubspotContactsSecond);

            var sut = new HubspotContactsCachePolicy(asyncCacheProvider, cachingConfig);
            var context = new Context($"{count}");

            var firstResult = await sut.ExecuteAsync(_ => func(count), context);
            var secondResult = await sut.ExecuteAsync(_ => func(count), context);
            await Task.Delay(TimeSpan.FromSeconds(ttl + 1));
            var thirdResult = await sut.ExecuteAsync(_ => func(count), context);

            firstResult.Should().BeEquivalentTo(hubspotContactsFirst);
            secondResult.Should().BeEquivalentTo(hubspotContactsFirst);
            thirdResult.Should().BeEquivalentTo(hubspotContactsSecond);

            A.CallTo(() => func(count)).MustHaveHappenedTwiceExactly();
        }

        [Theory, AutoFakeData]
        public async Task ExecuteAsync_ZeroTtl(
            int count,
            CachePolicyConfiguration cachingConfig,
            HubspotContacts hubspotContactsFirst,
            HubspotContacts hubspotContactsSecond,
            Func<int, Task<HubspotContacts>> func)
        {
            const int ttl = 0;
            cachingConfig.CacheDurationSec = ttl;
            var asyncCacheProvider = BootstrappedTestFixture.GetService<IAsyncCacheProvider>();
            A.CallTo(() => func(count))
                .Returns(hubspotContactsFirst).Once()
                .Then.Returns(hubspotContactsSecond);

            var sut = new HubspotContactsCachePolicy(asyncCacheProvider, cachingConfig);
            var context = new Context($"{count}");

            var firstResult = await sut.ExecuteAsync(_ => func(count), context);
            var secondResult = await sut.ExecuteAsync(_ => func(count), context);

            firstResult.Should().BeEquivalentTo(hubspotContactsFirst);
            secondResult.Should().BeEquivalentTo(hubspotContactsSecond);
            firstResult.Should().NotBeEquivalentTo(secondResult);

            A.CallTo(() => func(count)).MustHaveHappenedTwiceExactly();
        }


        [Theory, AutoFakeData]
        public async Task ExecuteAsync_DifferentListSize(
            int countFirst,
            int countSecond,
            CachePolicyConfiguration cachingConfig,
            HubspotContacts hubspotContactsFirst,
            HubspotContacts hubspotContactsSecond,
            Func<int, Task<HubspotContacts>> func)
        {
            const int ttl = 1;
            cachingConfig.CacheDurationSec = ttl;
            var asyncCacheProvider = BootstrappedTestFixture.GetService<IAsyncCacheProvider>();
            A.CallTo(() => func(countFirst)).Returns(hubspotContactsFirst);
            A.CallTo(() => func(countSecond)).Returns(hubspotContactsSecond);

            var sut = new HubspotContactsCachePolicy(asyncCacheProvider, cachingConfig);

            var firstResult =
                await sut.ExecuteAsync(_ => func(countFirst),
                    new Context($"{countFirst}"));
            var secondResult =
                await sut.ExecuteAsync(_ => func(countSecond),
                    new Context($"{countSecond}"));

            firstResult.Should().BeEquivalentTo(hubspotContactsFirst);
            secondResult.Should().BeEquivalentTo(hubspotContactsSecond);
            firstResult.Should().NotBeEquivalentTo(secondResult);

            A.CallTo(() => func(countFirst)).MustHaveHappenedOnceExactly();
            A.CallTo(() => func(countSecond)).MustHaveHappenedOnceExactly();
        }
    }
```

In the first test, we are checking if the cache is working for the time period that we have specified.
In the second one, we are checking if it's not caching when we set time to `0`.
And in the third one, we are checking if our values are not being overridden by another one with a different key.

### Retry policy
Add configuration for retry policy

```csharp
    public interface IRetryPolicyConfiguration
    {
        int MaxNumberOfRetries { get; }
        TimeSpan RetryBackoffPeriod { get; }
    }

    public class RetryPolicyConfiguration : IRetryPolicyConfiguration
    {
        public const string SectionName = "RetryPolicy";

        public int? NumberOfRetries { get; set; }
        public int MaxNumberOfRetries => NumberOfRetries ?? 3;

        public int? RetryBackoffPeriodMs { get; set; }
        public TimeSpan RetryBackoffPeriod => TimeSpan.FromMilliseconds(RetryBackoffPeriodMs ?? 1000);
    }
```

Add it to DI container.

```csharp        
    services.AddConfiguration<IRetryPolicyConfiguration, RetryPolicyConfiguration>(
        Configuration.GetSection(RetryPolicyConfiguration.SectionName));

```

And the RetryPolisy itself, this one can be global for all requests to hubspot.

```csharp
    public interface IRetryPolicy
    {
        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action);
    }
    public class RetryPolicy : IRetryPolicy
    {
        public IRetryPolicyConfiguration RetryPolicyConfiguration { get; }
        public ILogger<RetryPolicy> Logger { get; }
        private readonly AsyncPolicy _policyInternal;

        public RetryPolicy(
            IRetryPolicyConfiguration retryPolicyConfiguration, 
            ILogger<RetryPolicy> logger)
        {
            RetryPolicyConfiguration = retryPolicyConfiguration ?? throw new ArgumentNullException(nameof(retryPolicyConfiguration));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _policyInternal = Policy.Handle<ApiException>(response =>
                    response.StatusCode == HttpStatusCode.InternalServerError ||
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    response.StatusCode == HttpStatusCode.RequestTimeout)
                .WaitAndRetryAsync(retryPolicyConfiguration.MaxNumberOfRetries, duration =>
                        retryPolicyConfiguration.RetryBackoffPeriod,
                    (exception, duration, retryCount, context) =>
                    {
                        Logger.LogWarning(exception,
                            $"Request failed with statusCode {((ApiException)exception).StatusCode}, " +
                            $"waiting {duration.Milliseconds} ms before retry. Retry attempt {retryCount}");
                    });
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
        {
            if(action == null)  throw new ArgumentNullException(nameof(action));

            return _policyInternal.ExecuteAsync(action);
        }
```

The request will be retried when we will receive InternalServerError, TooManyRequests or RequestTimeout
Don't forget about DI container

```csharp
    services.AddSingleton<IRetryPolicy, RetryPolicy>();
```

### Testing retry policy
This time we need to add the ability to `AutoFakeData` to set multiple values for some of the properties that should be positive or negative.
To do that we need to create Customization that will recognize field/property/constructor parameter and set the proper value for that.

Let's add 3 new methods to `ReflectionHelper`

```csharp
 public static bool MatchesConstructorArgument(this object request,Type declaringType, string targetName, out Type targetType)
        {
            if (request is ParameterInfo parameterInfo &&
                parameterInfo.Member.DeclaringType == declaringType &&
                parameterInfo.Member.MemberType == MemberTypes.Constructor &&
                parameterInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetType = parameterInfo.ParameterType;
                return true;
            }

            targetType = null;
            return false;
        }

        public static bool MatchesProperty(this object request, Type declaringType, string targetName, out Type targetType)
        {
            if (request is PropertyInfo propertyInfo &&
                propertyInfo.DeclaringType == declaringType &&
                propertyInfo.MemberType == MemberTypes.Property &&
                propertyInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetType = propertyInfo.PropertyType;
                return true;
            }

            targetType = null;
            return false;
        }

        public static bool MatchesField(this object request, Type declaringType, string targetName, out Type targetType)
        {
            if (request is FieldInfo fieldInfo &&
                fieldInfo.DeclaringType == declaringType &&
                fieldInfo.MemberType == MemberTypes.Field &&
                fieldInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetType = fieldInfo.FieldType;
                return true;
            }

            targetType = null;
            return false;
        }
```

Create a customization

```csharp
 public class CustomizationsWithTargetValue : ICustomization, ISpecimenBuilder
    {
        public Type DeclaringType { get; }
        public string TargetName { get; }
        public object TargetValue { get; }

        public CustomizationsWithTargetValue(Type declaringType, string targetName, object targetValue)
        {
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            TargetValue = targetValue;
        }

        public object Create(object request, ISpecimenContext context)
        {
            if (request == null)
                return new NoSpecimen();

            if (request.MatchesConstructorArgument(DeclaringType, TargetName,out var constructorArgumentType))
                return CreateSpecimen(constructorArgumentType);
            if (request.MatchesProperty(DeclaringType, TargetName, out var propertyType))
                return CreateSpecimen(propertyType);
            if (request.MatchesField(DeclaringType, TargetName, out var fieldType))
                return CreateSpecimen(fieldType);

            return new NoSpecimen();
        }

        public object CreateSpecimen(Type targetType)
        {
            // Allow passing decimal as string since attributes do not support decimal values
            if (targetType == typeof(decimal) &&
                TargetValue is string)
            {
                return decimal.Parse(TargetValue?.ToString());
            }

            // Allow passing nullable decimal as string since attributes do not support decimal values
            if (targetType == typeof(decimal?) &&
                TargetValue is string)
            {
                return decimal.TryParse(TargetValue?.ToString(), out var result) ? result : (decimal?)null;
            }

            // Allow passing URI as string since attributes do not support complex types
            if (targetType == typeof(Uri) && TargetValue is string)
            {
                return TargetValue == null ? null : new Uri(TargetValue.ToString());
            }

            return TargetValue;
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }
```

We have to extend `AutoFakeCustomization` with the constructor that will handle additional Customizations

```csharp
    public AutoFakeCustomization(params ICustomization[] customizations)
        : base(customizations.Concat(new ICustomization[]
        {
            new AutoFakeItEasyCustomization {GenerateDelegates = true},
            new TestCustomization()
        }))
    {
    }
```

And the same case with `AutoFakeDataAttribute`

```csharp
    protected AutoFakeDataAttribute(params ICustomization[] customizations)
        : base(() => new Fixture().Customize(new AutoFakeCustomization(customizations)))

    {
    }
```

To use `CustomizationsWithTargetValue` with `AutoFakeDataAttribute` we need one more constructor

```csharp
    public AutoFakeDataAttribute(Type targetType, string argumentName, object argumentValue)
        : this(new CustomizationsWithTargetValue(targetType, argumentName, argumentValue))
    {
    }
```

Now when creating a new test case we can set a specific value for a field/property/constructor parameter of each instance of a given class.

Going back to the main topic, we can now create some test for the retry policy.

```csharp
 public class RetryPolicyTests : BaseAssertion<RetryPolicy>
    {
    [Theory]
    [AutoFakeData(typeof(HttpResponseMessage), "statusCode", HttpStatusCode.TooManyRequests)]
    [AutoFakeData(typeof(HttpResponseMessage), "statusCode", HttpStatusCode.InternalServerError)]
    [AutoFakeData(typeof(HttpResponseMessage), "statusCode", HttpStatusCode.RequestTimeout)]
    public async Task Execute_Retries(
        IRetryPolicyConfiguration retryPolicyConfiguration,
        Func<Task<string>> action,
        ILogger<RetryPolicy> logger,
        HttpRequestMessage httpRequestMessage,
        HttpResponseMessage httpResponseMessage,
        RefitSettings refitSettings)
    {
        const int numberOfRetries = 3;
        A.CallTo(() => retryPolicyConfiguration.RetryBackoffPeriod).Returns(TimeSpan.FromMilliseconds(100));
        A.CallTo(() => retryPolicyConfiguration.MaxNumberOfRetries).Returns(numberOfRetries);

        httpResponseMessage.StatusCode = HttpStatusCode.TooManyRequests;

        var sut = new RetryPolicy(retryPolicyConfiguration, logger);

        A.CallTo(() => action())
            .Throws(await ApiException.Create(httpRequestMessage, HttpMethod.Get, httpResponseMessage,
                refitSettings));

        await Assert.ThrowsAsync<ApiException>(async () => await sut.ExecuteAsync(action));

        A.CallTo(() => action())
            .MustHaveHappenedANumberOfTimesMatching(x => x == numberOfRetries + 1);
    }

    [Theory]
    [AutoFakeData(typeof(RetryPolicyConfiguration), "RetryBackoffPeriodMs", 500)]
    [AutoFakeData(typeof(RetryPolicyConfiguration), "RetryBackoffPeriodMs", 750)]
    [AutoFakeData(typeof(RetryPolicyConfiguration), "RetryBackoffPeriodMs", 1000)]
    public async Task Execute_RespectsBackoffPeriod(IRetryPolicyConfiguration retryPolicyConfiguration,
        ILogger<RetryPolicy> logger, Func<Task<string>> action, HttpRequestMessage httpRequestMessage,
        HttpResponseMessage httpResponseMessage, RefitSettings refitSettings)
    {
        const int faultToleranceMs = 25;
        const int numberOfRetries = 1;

        var firstCall = DateTime.Now;
        var secondCall = DateTime.Now;

        A.CallTo(() => retryPolicyConfiguration.MaxNumberOfRetries).Returns(numberOfRetries);

        httpResponseMessage.StatusCode = HttpStatusCode.RequestTimeout;

        var sut = new RetryPolicy(retryPolicyConfiguration, logger);

        var apiException =
            await ApiException.Create(httpRequestMessage, HttpMethod.Get, httpResponseMessage, refitSettings);

        A.CallTo(() => action())
            .Invokes(() => { firstCall = DateTime.Now; }).Throws(apiException).Once().Then
            .Invokes(() => { secondCall = DateTime.Now; }).Throws(apiException);

        await Assert.ThrowsAsync<ApiException>(async () => await sut.ExecuteAsync(action));
        firstCall.Should().BeCloseTo(
            secondCall.Subtract(retryPolicyConfiguration.RetryBackoffPeriod),
            faultToleranceMs);
    }
```

The first test will check if our policy is actually retrying for specified HTTP status codes.
The second one is checking the time delay between each retry.

### Using Policies
To use both policies in one place and don't worry about then later we will create a facade for our HTTP client. It will also take care of  `HapiKey` from the hubspot configuration.
For fetching contacts from hubspot we will use both cache+retry policies but for sending new contact to hubspot we will use only a retry policy. There is no need to cache a response from creating a contact.

```csharp
public interface IHubspotClientFacade
    {
        Task<HubspotContacts> GetContactsAsync(int count);
        Task<HubspotContact> CreateContactsAsync(CreateHubspotContact contacts);
    }

    public class HubspotClientFacade : IHubspotClientFacade
    {
        public IHubspotClient HubspotClient { get; }
        public IHubspotConfiguration HubspotConfiguration { get; }
        public IHubspotContactsCachePolicy HubspotContactsCachePolicy { get; }
        public IRetryPolicy RetryPolicy { get; }

        public HubspotClientFacade(
            IHubspotClient hubspotClient,
            IHubspotConfiguration hubspotConfiguration,
            IHubspotContactsCachePolicy hubspotContactsCachePolicy,
            IRetryPolicy retryPolicy)
        {
            HubspotClient = 
                hubspotClient ?? throw new ArgumentNullException(nameof(hubspotClient));
            HubspotConfiguration =
                hubspotConfiguration ?? throw new ArgumentNullException(nameof(hubspotConfiguration));
            HubspotContactsCachePolicy = 
                hubspotContactsCachePolicy ?? throw new ArgumentNullException(nameof(hubspotContactsCachePolicy));
            RetryPolicy = 
                retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        }

        public Task<HubspotContacts> GetContactsAsync(int count)
        {
            return HubspotContactsCachePolicy.ExecuteAsync(
                context => GetContactsInternalAsync(count),
                new Context($"HubspotContacts_{count}"));
        }

        public Task<HubspotContacts> GetContactsInternalAsync(int count)
        {
            return ExecuteApiCallAsync(() => HubspotClient.GetContactsAsync(HubspotConfiguration.HapiKey, count));
        }

        public Task<HubspotContact> CreateContactsAsync(
            CreateHubspotContact contacts)
        {
            if(contacts == null) throw new ArgumentNullException(nameof(contacts));
            return ExecuteApiCallAsync(() => HubspotClient.CreateContactsAsync(HubspotConfiguration.HapiKey, contacts));
        }

        public Task<T> ExecuteApiCallAsync<T>(Func<Task<T>> action)
        {
            if(action == null) throw new ArgumentNullException(nameof(action));
            return ExecuteApiCallInternalAsync(action);
        }

        private async Task<T> ExecuteApiCallInternalAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await RetryPolicy.ExecuteAsync(action);
            }
            catch (ApiException ae)
            {
                throw new HubspotContactsApiException(ae.StatusCode, ae.Message);
            }
        }
    }
```

Now our call will be retried when things go wrong as we can expect and a response from that call will be cached for a few seconds.
When API call will fail too many times we will cache `ApiException` and format our own `HubspotContactsApiException`.

```csharp
    public class HubspotContactsApiException : Exception
    {
        public HubspotContactsApiException(HttpStatusCode statusCode, string message)
            :base($"HubspotContacts request failed with statusCode: {statusCode}" +
                  $" with message: {message}")
        {
        }
    }
```

We have to test our facade now.

### Testing facade
In this case, we are only checking a flow. If all methods that should are called and if a proper exception is being thrown.

```csharp
    public class HubspotClientFacadeTests : BaseAssertion<HubspotClientFacade>
    {
        [Theory, AutoFakeData]
        public async Task GetContactsAsync_Ok(
            HubspotClientFacade sut,
            HubspotContacts hubspotContacts,
            int count)
        {
            A.CallTo(() =>
                    sut.HubspotContactsCachePolicy.ExecuteAsync(A<Func<Context, Task<HubspotContacts>>>._,
                        A<Context>._))
                .Returns(hubspotContacts);

            await sut.GetContactsAsync(count);

            A.CallTo(() =>
                    sut.HubspotContactsCachePolicy.ExecuteAsync(A<Func<Context, Task<HubspotContacts>>>._,
                        A<Context>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory, AutoFakeData]
        public async Task GetContactsInternalAsync_Ok(
            HubspotClientFacade sut,
            HubspotContacts hubspotContacts,
            int count)
        {
            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContacts>>>._))
                .Returns(hubspotContacts);

            await sut.GetContactsInternalAsync(count);

            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContacts>>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory, AutoFakeData]
        public async Task CreateContactsAsync_Ok(
            HubspotClientFacade sut,
            CreateHubspotContact contacts,
            HubspotContact response)
        {
            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContact>>>._))
                .Returns(response);

            await sut.CreateContactsAsync(contacts);

            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContact>>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory, AutoFakeData]
        public async Task ExecuteApiCallAsync_ThrowsHubspotContactsApiException(
            HubspotClientFacade sut,
            HttpRequestMessage httpRequestMessage,
            HttpResponseMessage httpResponseMessage,
            RefitSettings refitSettings,
            Func<Task<int>> action
        )
        {
            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<int>>>._))
                .Throws(await ApiException.Create(httpRequestMessage, HttpMethod.Get, httpResponseMessage,
                    refitSettings));

            await Assert.ThrowsAsync<HubspotContactsApiException>(async () => await sut.ExecuteApiCallAsync(action));
        }
    }
```

## Service
 *switch to tag `stage8-service` to have a complete solution of this stage*

Now we have all pieces together so we can create a simple service that will use mappers, facade and repository to do the job we want.

```csharp
    public interface IHubspotService
    {
        Task<ContactsList> GetHubspotContactsAsync(int count);
        Task<int> CreateHubspotContactAsync(CreateContact createContact);
    }

    public class HubspotService
    {
        public IHubspotClientFacade HubspotClientFacade { get; }
        public IHubspotMapper HubspotMapper { get; }
        public IUserRepository UserRepository { get; }

        public HubspotService(IHubspotClientFacade hubspotClientFacade, IHubspotMapper hubspotMapper,
            IUserRepository userRepository)
        {
            HubspotClientFacade = hubspotClientFacade ?? throw new ArgumentNullException(nameof(hubspotClientFacade));
            HubspotMapper = hubspotMapper ?? throw new ArgumentNullException(nameof(hubspotMapper));
            UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public Task<ContactsList> GetHubspotContactsAsync(int count)
            => GetHubspotContactsInternalAsync(count);

        private async Task<ContactsList> GetHubspotContactsInternalAsync(int count)
        {
            var hubspotContacts = await HubspotClientFacade.GetContactsAsync(count);
            return HubspotMapper.FromHubspotContacts(hubspotContacts);
        }

        public Task<int> CreateHubspotContactAsync(CreateContact createContact)
        {
            if (createContact == null) throw new ArgumentNullException(nameof(createContact));
            return CreateHubspotContactInternalAsync(createContact);
        }

        private async Task<int> CreateHubspotContactInternalAsync(CreateContact createContact)
        {
            if (createContact == null) throw new ArgumentNullException(nameof(createContact));

            var hubspotContact = HubspotMapper.ToCreateHubspotContact(createContact);
            var response = await HubspotClientFacade.CreateContactsAsync(hubspotContact);
            var contact = HubspotMapper.FromHubspotContact(response);
            return await UserRepository.CreateUserAsync(contact.ExternalId, contact.Email);
        }
    }
```

And DI container

```csharp
    services.AddTransient<IHubspotService, Services.HubspotService>();
```

### Testing service
Service is simple so our test will also be simple. We are checking the flow because all classes that we are using are covered by their own tests.

```csharp
    public class HubspotServiceTests : BaseAssertion<HubspotService.Services.HubspotService>
    {
        [Theory, AutoFakeData]
        public async Task GetHubspotContactsAsync_Ok(
            HubspotService.Services.HubspotService sut,
            int count,
            HubspotContacts hubspotContacts,
            ContactsList contactsList)
        {
            A.CallTo(() => sut.HubspotClientFacade.GetContactsAsync(count))
                .Returns(hubspotContacts);
            A.CallTo(() => sut.HubspotMapper.FromHubspotContacts(hubspotContacts))
                .Returns(contactsList);

            var response = await sut.GetHubspotContactsAsync(count);

            A.CallTo(() => sut.HubspotClientFacade.GetContactsAsync(count))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.HubspotMapper.FromHubspotContacts(hubspotContacts))
                .MustHaveHappenedOnceExactly();

            response.Should().BeEquivalentTo(contactsList);
        }

        [Theory, AutoFakeData]
        public async Task CreateHubspotContactAsync_Ok(
            HubspotService.Services.HubspotService sut,
            CreateContact createContact,
            CreateHubspotContact createHubspotContact,
            HubspotContact hubspotContact,
            Contact contact,
            int id
        )
        {
            A.CallTo(() => sut.HubspotMapper.ToCreateHubspotContact(createContact))
                .Returns(createHubspotContact);
            A.CallTo(() => sut.HubspotClientFacade.CreateContactsAsync(createHubspotContact))
                .Returns(hubspotContact);
            A.CallTo(() => sut.HubspotMapper.FromHubspotContact(hubspotContact))
                .Returns(contact);
            A.CallTo(() => sut.UserRepository.CreateUserAsync(contact.ExternalId, contact.Email))
                .Returns(id);

            var response = await sut.CreateHubspotContactAsync(createContact);

            A.CallTo(() => sut.HubspotMapper.ToCreateHubspotContact(createContact))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.HubspotClientFacade.CreateContactsAsync(createHubspotContact))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.HubspotMapper.FromHubspotContact(hubspotContact))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.UserRepository.CreateUserAsync(contact.ExternalId, contact.Email))
                .MustHaveHappenedOnceExactly();

            Assert.Equal(id, response);
        }
    }
```

## Controler and validation
In this stage, we will create a controller with two endpoints for getting contacts and for creating a new one. For the second one, we also use `FlientValidation` to validate what we are receiving.
Let's begin with creating `HubspotController`.

```csharp
    [Route("api/hubspot")]
    [ApiController]
    public class HubspotController : ControllerBase
    {
        public IHubspotService HubspotService { get; }
        public HubspotController(IHubspotService hubspotService)
        {
            HubspotService = hubspotService ?? throw new ArgumentNullException(nameof(hubspotService));
        }

        [HttpGet]
        public async Task<IActionResult> GetContactsAsync(int count)
        {
            return Ok(await HubspotService.GetHubspotContactsAsync(count));
        }

        [HttpPost]
        public async Task<IActionResult> CreateContactAsync(CreateContact createContact)
        {
            await HubspotService.CreateHubspotContactAsync(createContact);
            return Ok();
        }
    }
```

For `CreateContact` model we have to create a validator. Please install `FluentValidation.AspNetCore` and create Validator.

```csharp
    public class CreateContactValidator : AbstractValidator<CreateContact>
    {
        public CreateContactValidator()
        {
            RuleFor(contact => contact)
                .NotNull();

            RuleFor(contact => contact.FirstName)
                .NotNull()
                .NotEmpty()
                .MinimumLength(2);

            RuleFor(contact => contact.Email)
                .NotNull()
                .NotEmpty()
                .EmailAddress()
                .Must(m => m != null && m.EndsWith("@softwarehut.com"))
                .WithMessage("'{PropertyName}' should ends with @softwarehut.com");
        }
    }
```
Our validator will check if `FirstName` field contains at least 2 characters and if `Email` is from `softwarehut.com` domain.
The last thing is to add this validator to DI container add Newtonsoft as a JSON serializer. 
Install `Microsoft.AspNetCore.Mvc.NewtonsoftJson` and use extension methods fron Newtonsoft and add FluentValidation.

```csharp
    services
        .AddMvc()
        .AddNewtonsoftJson()
        .AddFluentValidation();

    services.AddSingleton<IValidator<CreateContact>, CreateContactValidator>();
```

### Testing fluent validation
To test fluent validation we need to use `FluentValidation.Validators.UnitTestExtension` nuget package.
We need to check if propper validation rules were added to all fields and if our custom e-mail address domain rule is working.

```csharp
public class CreateContactValidatorTests
    {
        private readonly CreateContactValidator sut = new CreateContactValidator();

        [Fact]
        public void Class_IsConfiguredCorrectly()
        {
            sut.ShouldHaveRules(x => x,
                BaseVerifiersSetComposer.Build()
                    .AddPropertyValidatorVerifier<NotNullValidator>()
                    .Create());
        }

        [Fact]
        public void FirstName_IsConfiguredCorrectly()
        {
            sut.ShouldHaveRules(x => x.FirstName,
                BaseVerifiersSetComposer.Build()
                    .AddPropertyValidatorVerifier<NotNullValidator>()
                    .AddPropertyValidatorVerifier<NotEmptyValidator>()
                    .AddMinimumLengthValidatorVerifier(2)
                    .Create());
        }

        [Fact]
        public void Email_IsConfiguredCorrectly()
        {
            sut.ShouldHaveRules(x => x.Email,
                BaseVerifiersSetComposer.Build()
                    .AddPropertyValidatorVerifier<NotNullValidator>()
                    .AddPropertyValidatorVerifier<NotEmptyValidator>()
                    .AddPropertyValidatorVerifier<AspNetCoreCompatibleEmailValidator>()
                    .AddPropertyValidatorVerifier<PredicateValidator>()
                    .Create());
        }

        [Theory]
        [AutoFakeData(typeof(CreateContact), "Email", "test@softwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test.test@softwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test_test@softwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test2@softwarehut.com")]
        public void ValidEmail_PassesValidation(CreateContact contact)
        {
            sut.ShouldNotHaveValidationErrorFor(x => x.Email, contact);
        }

        [Theory]
        [AutoFakeData(typeof(CreateContact), "Email", "test@s_oftwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test@softwarehut.pl")]
        [AutoFakeData(typeof(CreateContact), "Email", "test@hut.cpm")]
        public void InvalidEmail_PassesValidation(CreateContact contact)
        {
            sut.ShouldHaveValidationErrorFor(x => x.Email, contact);
        }
    }
```

### Testing Controller
To verify GuardClauseAssertion for a `ControllerBase` classes we can't use our `BaseAssertion`.
We need to create an attribute that will force `AutoFixture` to use the most greedy constructor.

```csharp
    public class GreedyControllerConstructorAttribute : AutoFakeDataAttribute
    {
        public GreedyControllerConstructorAttribute()
            : base(new ConstructorCustomization(typeof(ControllerBase), new GreedyConstructorQuery()))
        {
        }
    }
```

For other tests we can user build-in `GreedyAttribute`
To pass our validation we need to make sure that e-mail address for `CreateContact` will always ends with `@softwarehut.com`.
Let's add it to `TestCustomization`.

```csharp
    fixture.Customizations.Add(
        new ConstructorArgumentSpecimen<CreateContact, string>(
            "email", () => $"{Faker.Name.First()}@softwarehut.com"));
```

```csharp
    public class HubspotControllerTests
    {
        [Theory, GreedyControllerConstructor]
        public void SutHasGuardClauses(GuardClauseAssertion guardClauseAssertion)
        {
            guardClauseAssertion.Verify(typeof(HubspotController).GetConstructors());
            guardClauseAssertion.Verify(typeof(HubspotController)
                .GetMethods(BindingFlags.DeclaredOnly));
        }

        [Theory, AutoFakeData]
        public async Task GetContactsAsync_Ok(
            [Greedy] HubspotController sut,
            int count,
            ContactsList response)
        {
            A.CallTo(() => sut.HubspotService.GetHubspotContactsAsync(count))
                .Returns(response);

            var result = await sut.GetContactsAsync(count);

            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var getContactsResponseResult = Assert.IsType<ContactsList>(okObjectResult.Value);
            getContactsResponseResult.Should().BeEquivalentTo(response);
            A.CallTo(() => sut.HubspotService.GetHubspotContactsAsync(count))
                .MustHaveHappened();
        }

        [Theory, AutoFakeData]
        public async Task CreateContactAsync_Ok(
            [Greedy] HubspotController sut,
            CreateContact request,
            int response)
        {
            A.CallTo(() => sut.HubspotService.CreateHubspotContactAsync(request))
                .Returns(response);

            var result = await sut.CreateContactAsync(request);

            Assert.IsType<OkResult>(result);
            A.CallTo(() => sut.HubspotService.CreateHubspotContactAsync(request))
                .MustHaveHappened();
        }
    }
```
We have verified a flow so it's high time to add some integration test.

### Integration tests
*switch to tag `stage9-controller` to have a complete solution for this stage*

We need a few things in our `BootstrappedTestFixture` class.
* initialize an empty test database - I will use one from docker
* make sure that the database is removed after tests
* mock response from hubspot api
* spin up webserver
* replace two environment variables

We have to add `WireMock.Net` nuget.

Complete `BootstrappedTestFixture` class will look like that:
```csharp
public class BootstrappedTestFixture : WebApplicationFactory<Startup>, IAsyncLifetime
{
    public HttpClient TestClient { get; }
    public WireMockServer WireMockServer { get; }
    public HubspotDbContext HubspotDbContext { get; }

    private string ConnectionString { get; }
    public string Hapikey = "demo";

    public BootstrappedTestFixture()
    {
        WireMockServer = WireMockServer.Start();
        Environment.SetEnvironmentVariable("Hubspot__baseUrl", $"http://localhost:{WireMockServer.Ports.First()}");
        Environment.SetEnvironmentVariable("Hubspot__hapikey", Hapikey);    

        ConnectionString = "Server=127.0.0.1,1401;" +
                            $"Database=hubspot_{Guid.NewGuid():N};" +
                            "User Id=SA;" +
                            "Password=YourSTRONG!Passw0rd;" +
                            "MultipleActiveResultSets=True";

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);

        WithWebHostBuilder(b =>
        {
            b.UseConfiguration(InitConfiguration())
                .UseStartup<Startup>();
        });

        HubspotDbContext = InitializeDbContext();
        TestClient = CreateClient();
    }

    public HubspotDbContext InitializeDbContext()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkSqlServer()
            .BuildServiceProvider();

        var builder = new DbContextOptionsBuilder<HubspotDbContext>();

        builder.UseSqlServer(ConnectionString)
            .UseInternalServiceProvider(serviceProvider);

        var context = new HubspotDbContext(builder.Options);
        context.Database.Migrate();
        return context;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => HubspotDbContext.Database.EnsureDeletedAsync();
    public T GetService<T>() => Services.GetRequiredService<T>();

    private static IConfiguration InitConfiguration() =>
        new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
}
```

Now we are ready to add integration tests that will verify if everything is working as expected and after creating a hubspot contact we have it in our database.

```csharp
    [IntegrationTests]
    [Collection(BootstrappedTestCollection.CollectionName)]
    public class HubspotControllerTests
    {
        public BootstrappedTestFixture BootstrappedTestFixture { get; }

        public string GetContactsPath = "/contacts/v1/lists/all/contacts/all";
        public string CreateContactPath = "/contacts/v1/contact";
        public string ApiPath = "api/hubspot";

        public HubspotControllerTests(
            BootstrappedTestFixture bootstrappedTestFixture)
        {
            BootstrappedTestFixture = bootstrappedTestFixture ??
                                      throw new ArgumentNullException(nameof(bootstrappedTestFixture));
        }

        [Theory, AutoFakeData]
        public async Task GetHubspotContactsAsync_Ok(
            int count,
            HubspotContacts hubspotContacts)
        {
            BootstrappedTestFixture.WireMockServer
                .Given(Request.Create()
                    .WithPath(GetContactsPath)
                    .WithParam("hapikey", BootstrappedTestFixture.Hapikey)
                    .WithParam("count", count.ToString())
                    .UsingGet())
                .RespondWith(
                    Response.Create().WithStatusCode(200)
                        .WithBody(JsonConvert.SerializeObject(hubspotContacts)));

            var response = await BootstrappedTestFixture.TestClient
                .GetAsync($"{ApiPath}?count={count}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseJsonString = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(responseJsonString);

            var exceptionResponse = JsonConvert.DeserializeObject<ContactsList>(responseJsonString);
            Assert.NotNull(exceptionResponse);
        }

        [Theory, AutoFakeData]
        public async Task CreateContactAsync_Ok(
            int externalId,
            CreateContact contact)
        {
            var hubspotContact = new HubspotContact(
                externalId,
                new List<HubspotProfile>
                {
                    new HubspotProfile(
                        new List<HubspotIdentity>
                        {
                            new HubspotIdentity("EMAIL", contact.Email)
                        })
                });

            BootstrappedTestFixture.WireMockServer
                .Given(Request.Create()
                    .WithPath(CreateContactPath)
                    .WithParam("hapikey", BootstrappedTestFixture.Hapikey)
                    .UsingPost())
                .RespondWith(
                    Response.Create().WithStatusCode(200)
                        .WithBody(JsonConvert.SerializeObject(hubspotContact)));

            var content = new StringContent(JsonConvert.SerializeObject(contact), Encoding.UTF8, "application/json");
            var response = await BootstrappedTestFixture.TestClient.PostAsync(ApiPath, content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var user = await BootstrappedTestFixture.HubspotDbContext.Users.Where(x => x.Email == contact.Email)
                .FirstOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal(contact.Email, user.Email);
            Assert.Equal(externalId, user.ExternalId);
        }
    }
```

This the end. We have tests for each part of our code and we have an integrations test that is verifying a full functionality.


