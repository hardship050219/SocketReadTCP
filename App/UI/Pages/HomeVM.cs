namespace AppZC.UI.Pages;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
[ApiController("/vm/home")]
public class HomeVM
{
	[HttpPost(Role = "sm,usr")]
	public ApiResult<UserLoginResponse> Login([FromBody] UserLoginRequest request)
	{
		if (request.Username == "admin" && request.Password == "admin")
			return new UserLoginResponse($"ADMIN-TK::{DateTime.Now}");
		return ApiResult.Err<UserLoginResponse>("Wrong username or password");
	}

	public record UserLoginRequest(string Username, string Password);

	public record UserLoginResponse(string? Token);
}