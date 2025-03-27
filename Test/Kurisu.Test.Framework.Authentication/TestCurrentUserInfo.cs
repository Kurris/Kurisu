using Xunit;

namespace Kurisu.Test.Framework.Authentication;

[Trait("Auth", "CurrentUserInfo")]
public class TestCurrentUserInfo
{
    private readonly string _token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IkVCODQxRDM2RTA1OUVBQzFGMDY3RkQzQjJCNjkyMEQ3IiwidHlwIjoiYXQrand0In0.eyJuYmYiOjE2NjAwMjY0MzQsImV4cCI6MTY2MDAzMDAzNCwiaXNzIjoiaWRlbnRpdHkuaXNhd2Vzb21lLmNuIiwiYXVkIjoid2VhdGhlciIsImNsaWVudF9pZCI6InNwYSIsInN1YiI6IjMiLCJhdXRoX3RpbWUiOjE2NjAwMjY0MjEsImlkcCI6ImxvY2FsIiwianRpIjoiQUU5MDI4NzEyNTFDRERFQjFDNThEQ0Q3M0UyMTlEQzMiLCJzaWQiOiJDRkQwNzkyMTU4MzJGNjdBQjZFNTkzMzEyQjgwMDMyMyIsImlhdCI6MTY2MDAyNjQzNCwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsIndlYXRoZXI6c2VhcmNoIiwib2ZmbGluZV9hY2Nlc3MiXSwiYW1yIjpbInB3ZCJdfQ.S3q77NoIG0ZoU9EoRg7tKUvDKwAFnfkelEgh8bCaLjAxCXYV0NGiK_2nTfwC10WgjZnXPw0ZXT9ZF_oPKZMEOAghICKnYLAfOhlS8nQIaVuBxqZEzodXhp-VlZ04KJUeWyux0DXqZsrvv7gXitIU_IknncZWvNHO4zHrV7YgLqOrrR6nBVK3M6eGfQ7KTsJg9smON2izMCTb6vFbTSzIMDVrdJiymyokaQkAKolU0-kIRbWyI8ilSjZrnjvOomw_q78hCfBVBk0W4Tyf1M9mXHfwfpOIswlozfWjDm85zvR4HK7hechTogaPzMjIFmIMOMn_rqZJKVlIhqXB1cFvww";

    [Fact]
    public void GetSubject_Return_3()
    {
        var sub = TestHelper.GetResolver(_token).GetUserId();
        Assert.Equal(3, sub);
    }


    [Fact]
    public void GetBearerToken()
    {
        var accessToken = TestHelper.GetResolver(_token).GetToken();
        Assert.Equal("Bearer " + _token, accessToken);
    }
}