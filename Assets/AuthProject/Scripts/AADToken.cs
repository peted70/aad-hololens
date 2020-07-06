public class AADToken : IToken
{
	public AADToken(string token)
	{
		Token = token;
	}
    public string Token { get; private set; }
}
