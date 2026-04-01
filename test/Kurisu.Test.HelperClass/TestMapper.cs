using Kurisu.AspNetCore.Abstractions.ObjectMapper;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.HelperClass;

public class TestMapper
{
    [Fact]
    public void TestMap()
    {
        var services = new ServiceCollection();
        services.AddObjectMapper();
        var serviceProvider = services.BuildServiceProvider();

        var userDto = new UserDto()
        {
            Id = 1, Age = 18, Name = "ligy", ImageUrls = new List<string>() { "https://" }
        };

        var user = userDto.Map<User>();
        Assert.Equal(userDto.Id, user.Id);
        Assert.Equal(userDto.Name, user.Name);
        Assert.Equal(userDto.Age, user.Age);
        Assert.Equal(new List<string>() { "https://" }.ToJson(), user.ImageUrls);

        var userPatch = new UserDtoForPatch()
        {
            ImageUrls = new List<string>() { "https://abc.com" }
        };

        userPatch.MapToIgnoreNull(user);
        Assert.Equal(userDto.Id, user.Id);
        Assert.Equal(userDto.Name, user.Name);
        Assert.Equal(userDto.Age, user.Age);
        Assert.Equal(new List<string>() { "https://abc.com" }.ToJson(), user.ImageUrls);

        userPatch.MapTo(user);
        Assert.Equal(0, user.Id);
        Assert.Null(user.Name);
        Assert.Equal(0, user.Age);
        Assert.Equal(new List<string>() { "https://abc.com" }.ToJson(), user.ImageUrls);
    }


    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public string ImageUrls { get; set; }
    }

    public class UserDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public List<string> ImageUrls { get; set; }
    }

    public class UserDtoForPatch
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }

        public List<string> ImageUrls { get; set; }
    }
}