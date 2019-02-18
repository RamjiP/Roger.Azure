- [ Introduction ](#introduction)
- [ Configuration ](#configuration)
- [ DB Context ](#db-context)
- [ Collection Name Attribute ](#collection-name-attribute)
- [ SQL Query ](#sql-query)
- [ Examples ](#examples)

# Introduction

**Roger.Azure.Cosmos** has a generic implementation of repository pattern for accessing Cosmos DB SQL API in C#.  It provides a base repository ```DocuentDbRepository<T>``` that exposes functions to perform CRUD operation on a document collection.

# Configuration

The configurations are injected using ```IOptions<DocumentDbConfiguration>```.  This can be achieved by adding following lines in ```Startup.cs```

``` c#
    public void ConfigureServices(IServiceCollection services)
    {
        // ........
        services.Configure<DocumentDbConfiguration>(Configuration.GetSection("Persistence:Cosmos"));
        // ........
    }
```

and in appsettings.json

``` json
    {
        "Persistence": {
            "Cosmos": {
                "EndpointUrl": "http://localhost:8081",
                "PrimaryKey": "abcdefghijklmnop==",
                "DatabaseName": "Test"
            }
        }
    }
```

The content of DocumentDbConfiguration is

``` c#
    public class DocumentDbConfiguration
    {
        public string EndpointUrl { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseName { get; set; }

        public Uri EndpointUri => new Uri(EndpointUrl);
    }
```

| Property     | Description                                          |
| ------------ | ---------------------------------------------------- |
| EndpointUrl  | Url of the Cosmos DB                                 |
| PrimaryKey   | Primary key of the Cosmos DB                         |
| DatabaseName | Name of the database need to be created in Cosmos DB |

# DB Context

In ```DocumentDbContext``` class, it creates ```DocumentDbClient``` and ```Database``` if not exists.  Hence, it is better to register as a singleton service like

``` c#
services.AddSingleton<IDocumentDbContext, DocumentDbContext>();
```

# Collection Name Attribute

This is to define the name of the collection for each repository.  For eg.

``` c#

    [CollectionName("account")]
    public class AccountRepository : DocumentDbRepository<Account>
    {
        public AccountRepository(IDocumentDbContext context, ILogger<AccountRepository> logger) 
          : base(context, logger) 
          {

          }
    }
```

The above example would create ```account``` document collection in our Cosmos DB

# SQL Query

Here is an example to query against a property in a document collection inside Cosmos DB.

In ```UserRepository.cs```,
``` c#
    [CollectionName("user")]
    public class UserRepository : DocumentDbRepository<User>, IUserRepository
    {
        public UserRepository(IDocumentDbContext context, ILogger<UserRepository> logger) 
          : base(context, logger) 
          {

          }

        public async Task<User> GetByEmailAsync(string email) 
        {
            var sql = $"SELECT TOP 1 * FROM c WHERE c.email = '{email}'";
            var userResult = await GetAsync(sql, 1);
            return userResult.Data.FirstOrDefault();
        }
        
        public Task<TokenPagedResult<User>> GetByFirstNameAsync(string firstName) 
        {
            var sql = $"SELECT TOP 10 * FROM c WHERE CONTAINS(LOWER(c.firstName, '{firstName.ToLower()}'";
            return GetAsync(sql, 10);
        }
    }
```

In ```IUserRepsitory.cs```,

``` c#
    public interface IUserRepository: ITokenRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<TokenPagedResult<User>> GetByFirstNameAsync(string firstName);
    }
```

# Examples


Let's say ```User.cs``` has the following user model.

``` c#
    // C# Pascalcase Naming Stardard
    public class User 
    {
        string Id { get; set;}
        string Email { get; set;}
        string FirstName { get; set; }
        string LastName { get; set; }
    }
```

Since Cosmos DB documents require ```id``` field, we need to include the following line in ```StartUp.cs```

``` c#
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        JsonConvert.DefaultSettings = () => SerializerSettings.Default;
        ...
    }
```
Please refer ```Roger.Json``` Nuget package to get ```SerializerSettings```

In business logic, 

``` c#
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task MyBusinessLogicAsync()
        {
            var user = new User()
            {
                Email = "test@email.com",
                FirstName = "Test",
                LastName = "Surname"
            };

            // Defined in DocumentDbRepository
            user = _userRepository.CreateAsync(user);

            user.FirstName = "Test2";
            // Defined in DocumentDbRepository
            user = _userRepository.UpdateAsync(user);

            // Defined in DocumentDbRepository
            user = _userRepository.GetByIdAsync(user.Id);

            // Defined in UserRepository
            user = _userRepository.GetByEmailAsync("test@email.com");

            // Defined in UserRepository
            users = _userRepository.GetByFirstNameAsync("Test");
        }
    }
```
