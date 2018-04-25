[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Data/master/Shared/Icon.png "Zebble.Data"


## Zebble.Data

![logo]

A Zebble plugin that allow you to manage your data locally int your Zebble application.


[![NuGet](https://img.shields.io/nuget/v/Zebble.Data.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Data/)

> Sometimes the data is meant to be solely for used within the mobile app and there may not even be a web-based interface or admin screen to see or change the data, and so for all intents and purposes some of the data may be originated and used only within the mobile App. In such cases, you might be tempted to keep the data only locally on the mobile app and avoid bringing the server (DB) into the picture. Thus, this plugin help you to manage you local data.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Data/](https://www.nuget.org/packages/Zebble.Data/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

#### Database

To create a local database in Zebble application you need to set you database name in `Config.xml` file like below:
```xml
<Database.File>app.db3</Database.File>
```
Also, you can enable the cache mode of database by adding this code under the setting tag in `Config.xml`:
```xml
<Database.Cache.Enabled>true</Database.Cache.Enabled>
```

To create a database you need use this code:
```csharp
Zebble.Data.Setup.CreateDatabase();
```

##### Creating Tables

For creating tables you can create an object which inherited from `IEntity` interface like below:
```csharp
public class Person : IEntity
{
    [AutoNumber]
    public int Id { get; set; }

    public string Name { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public bool IsActivated { get; set; }
}
```
You should add this object under the **App.Domain\Entities**.

#### Database.Get()
This method has four overloads, requires an **"Entity Type"** to retrieve a record from and an **"ID"** to identify the record. This method returns single instance of the provided Entity Type and throws **"System.ArgumentNullException"**, if the input parameter is null or empty.

**Note:** The Entity Type must implement the **"IEntity"** Interface.

Below are the screenshots of the four overloads for this method:
```csharp
// Gets an Entity of the given type with the given Id from the database.
// If it can't find the object, an exception will be thrown.
var person = Database.Get<Domain.Entities.Person>(string entityId);
//Or
var person = Database.Get<Domain.Entities.Person>(Guid id);
//Or
var person = Database.Get<Domain.Entities.Person>(Guid? id);
//Or
var person = Database.Get<Domain.Entities.Person>(int? id);
```
This method must only be used when single record is required from database based on the **"ID"** of the entity.

#### Database.Find()
This method has 2 overloads. You also need to specify the Entity Type in order to utilize this method. This method returns First matched record of the provided Entity Type if found and returns **"null"** if no record is available in database based on the provided criteria. Below are the screenshots of two overloads for this method:

##### Overload 1
This overload requires one parameter of **"Criterion"** type collection. Criterion is Zebble internal class used to provide parameters and respective values to be searched against the database.

```csharp
Find<Entity>(params Criterion[] criteria);
```

##### Example:
We can call this method to get a record of Person by **"Name"**, as shown below

```csharp
var perosn = Database.Find<Domain.Entities.Person>(new Criterion("Name", FilterFunction.Is, "John"));
```

The code shown above will return an instance of Person entity type if there is an Person with name **"John"** or return **"null"** if no such Person exists in the database.

**Important:** Please note that this method takes the first record found based on the criteria provided for searching a record e.g. in case of above shown query, if there are more than one **"Person"** with the name of **"John"** then the first record will be return by this method.

##### Overload 2
This overload requires two parameters and is very useful when you not only want to search a record but also want to perform sorting, or ranging before fetching a record in SQLlite. The first parameter requires a **"Lambda Expression"** criteria and the second parameter **"Query Option"** is used to pass additional query options. e.g. sorting, ranging etc.

```csharp
Database.Find<Entity>(Expression<Func<Entity, bool>> criteria, params QueryOption[] options);
```

##### Example
We can follow the same example as we used for the first overload but this time we will use a Lambda Expression and Query Option as shown below:

```csharp
var person = Database.Find<Domain.Entities.Person>(p => p.Name == "John", options: QueryOption.OrderBy("Email"));
```

The code shown above will return an instance of Person entity type if there is an Person having name as "John" but this time all the Person records will be ordered by their **"Email"** before taking the first matching record.

#### Database.GetList()
This method gives you the possibility to get a list of data from the database and to apply filters. All the overloads of this method require an argument of Type and return an

##### Overload 1
```csharp
IEnumerable<Entity> GetList(Type type, params Criterion[] conditions);
```
This overload allows you to pass an array of Criterion.
##### Example:
```csharp
var criterions = new Criterion[] 
{
    new Criterion("Email",FilterFunction.EndsWith,"@geeks.ltd.uk"),
    new Criterion("IsActivated",FilterFunction.Is,true)
};
var personel = Database.GetList(typeof(Domain.Entities.Person), criterions);
```
Criterion is a class used to provide parameters, comparators and respective values to be searched in the database.

##### Overload 2
```csharp
IEnumerable<Entity> GetList(Type type, QueryOption[] queryOptions);
```
This overload allows you to pass an existing QueryOption, for example PagingQueryOption, or to pass a new one.
##### Example:
```csharp
var options = new[]
{
    new PagingQueryOption("ASC",0,20)
};
var personel = Database.GetList(typeof(Domain.Entities.Person), options);
```
##### Overload 3
```csharp
IEnumerable<IEntity> GetList(Type type, IEnumerable<Guid> ids);
```
This overload allows you to pass a list of Guid.
##### Example:
```csharp
var ids = new[]
{
    Guid.Empty
};
var personel = Database.GetList(typeof(Domain.Entities.Person), ids);
```
##### Overload 4
```csharp
IEnumerable<Entity> GetList(Type type, IEnumerable<ICriterion> criteria, QueryOption[] queryOptions);
```
This overload allows you to pass a list of Criterion and an array of QueryOption.
##### Example:
```csharp
var criterions = new Criterion[] 
{
    new Criterion("Email",FilterFunction.EndsWith,"@geeks.ltd.uk"),
    new Criterion("IsActivated",FilterFunction.Is,true)
};
var options = new[]
{
    new PagingQueryOption("ASC",0,20)
};
var personel = Database.GetList(typeof(Domain.Entities.Person), criterions, options);
```
#### Databas.Save() 

Zebble provided data persistence facility is not only limited to single entity instance but is also available for persisting a collection of records.
It provides six overloads of `Database.Save` method.

**Note:** Database. Save also returns the saved instance(s) to be used later in code.

##### Overloads

```csharp
Entity Save<Entity>(Entity entity);

IEnumerable<Entity> Save<Entity>(List<Entity> records);

IEnumerable<T> Save<T>(IEnumerable<T> records, SaveBehaviour behaviour)

void Save(IEntity entity, SaveBehaviour behaviour);
```

##### Examples

###### Overload 1 – Saving a New Entity Instance
```csharp
Database.Save(new Domain.Entities.Person 
{ Name = "John", LastName = "Wills", Email = "JohnWills@uat.co"
});
```
The code above demonstrates that we are saving a new instance of Person entity.

###### Overload 2 – Saving an Array of Entity:

Zebble runs bulk saves in **“Transaction”**. Meaning, failure to save one records results rolling back all the saved instances of the current collection

```csharp
Database.Save(new Domain.Entities.Person[]
{
    new Domain.Entities.Person{  Name = "John", LastName = "Wills", Email = "JohnWills@uat.co" },
    new Domain.Entities.Person{  Name = "John", LastName = "Wills", Email = "JohnWills@uat.co" },
    ...
});
```

###### Overload 3 – Saving a List of Entities by Specifying Save Behaviour
```csharp
Database.Save(new Domain.Entities.Person[]
{
    new Domain.Entities.Person{  Name = "John", LastName = "Wills", Email = "JohnWills@uat.co" },
    new Domain.Entities.Person{  Name = "John", LastName = "Wills", Email = "JohnWills@uat.co" },
    ...
}
, SaveBehaviour.BypassSaved);
```
The code shown above is saving a List of Person by specifying **“SaveBehaviour”**.

###### Overload 4 – Saving a sing Entity by Specifying Save Behaviour
```csharp
Database.Save(new Domain.Entities.Person
{
    Name = "John", LastName = "Wills", Email = "JohnWills@uat.co"
}
, SaveBehaviour.BypassSaved);
```

**Important:** Database.Save can also be used to update an existing record. Although, it is not recommended to call Database.Save method for updating an existing entity instance, but in exceptional cases, always **“Clone”** the entity instance before changing and then call Database.Save. by passing cloned object. The reason is that you are changing the object that is already referenced on data cache so If Save action fails the object on cache would be different than the one in database. (Memory Cache is not transactional). 

#### Database.Update()

Zebble provides an `Update()` method to update records in the database.

##### Overloads
There are two overload for the Update method, one for updating one instance and the other for updating a list of instances.

###### Overload 1: One instance
This overload allows you to update the properties of one entry.
```csharp
Entity Update<Entity>(Entity item, Action<Entity> action);
```
Example:
```csharp
Database.Update(person, p => p.IsActivated);
```
###### Overload 2: Multiple instances
This overload allows you to update the properties of a list of entries in one line and inside a transaction scope. If one update fails all updates will be rolled back.
```csharp
List<Entity> Update<Entity>(IEnumerable<Entity> items, Action<Entity> action);
```
Example:
```csharp
Database.Update(personel, p => p.IsActivated);
```

#### Database.Delete()
Zebble provides `Database.Delete` method for this purpose, which requires an entity instance to be deleted from database.
Zebble provides three overloads of `Database.Delete` method. 

##### Overloads - 1 Delete Single Person Record
Oevrload 1
```csharp
void Delete(IEntity instance);
```
Example
```csharp
Database.Delete(person);
```
##### Overloads - 2 Delete Collection of Person Records
Oevrload 2
```csharp
void Delete<Entity>(IEnumerable<Entity> instances);
```
Example
```csharp
Database.Delete(personel);
```
##### Overloads - 3 Delete Single Person with Delete behaviour
Oevrload 3
```csharp
void Delete(IEntity instance, DeleteBehaviour behaviour);
```
Example
```csharp
Database.Delete(person, DeleteBehaviour.BypassDeleted);
```

#### Database.Any(), None()

##### Any()
This method has two overloads and is used with conditional statements where we need to check if the resulting sequence is not empty

###### Overload 1
The first overload of this method doesn’t require any criteria / condition and is used just to determine if a sequence holds any elements, as shown below:

```csharp
bool Any<Entity>();
```
Example
```csharp
if(Database.Any<Domain.Entities.Person>())
{
    //Do something
}
```

###### Overload 2
The second overload requires a condition statement and determines if any element of a sequence satisfies the condition. The supplied condition can contain any number of logical operators.

```csharp
bool Any<Entity>(Expression<Func<Entity, bool>> criteria);
```
Example
```csharp
if(Database.Any<Domain.Entities.Person>(p=> p.IsActivated))
{
    //Do something
}
```

##### None()
This method has also two overloads and is also used with conditional statements where we need to check if the resulting sequence is empty.

###### Overload 1
The first overload of this method doesn’t require any criteria / conditions and is used just to determine if sequence holds no elements, as shown below:

```csharp
bool None<Entity>();
```
Example
```csharp
if(Database.None<Domain.Entities.Person>())
{
    //Do something
}
```

###### Overload 2
The second overload requires a condition statement and determines if a sequence contains no elements which satisfy the condition. The supplied condition can contain any number of logical operators.

```csharp
bool None<T>(Expression<Func<T, bool>> criteria)
```
Example
```csharp
if(Database.None<Domain.Entities.Person>(p=> p.IsActivated))
{
    //Do something
}
```

#### DataAccessor

In the majority of your applications you will not have to use this, but in some specific cases where the construction of the SQL query is important and performance is needed DataAccessor provides the functionality .
Keep in mind that by using this class you bypass the cache system used by Zebble, which means that if your query changed any data, you will have to refresh the cache by calling the method
`Database.Refresh()`

##### ExecuteNonQuery

ExecuteNonQuery executes a Transact-SQL statement against the connection and returns the number of rows affected.
```csharp
var query = "update Personel set IsActivated = 1 where IsActivate = 0";
var result = DataAccessor.ExecuteNonQuery(query);
Database.Refresh();
```

##### ExecuteReader

ExecuteReader executes the specified command text against the database connection of the context and builds an IDataReader.

**Important:** Make sure you close the data reader after finishing the required logic.

```csharp
var results = DataAccessor.ExecuteReader(query, System.Data.CommandType.Text);
//Or
var results = DataAccessor.ExecuteReader(query, System.Data.CommandType.Text,parameter1,...);
```

##### ExecuteScalar

ExecuteScalar executes the specified command text against the database connection of the context, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.

```csharp
var result = DataAccessor.ExecuteScalar<int>(query, System.Data.CommandType.Text);
//Or
var result = DataAccessor.ExecuteScalar<int>(query, System.Data.CommandType.Text,parameter1,...);
```