using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace MentorBot
{
    // Interface for generating a secure token.
    public interface ITokenGenerator
  {
    public string CreateToken();
  }

  public class TokenGenerator : ITokenGenerator
  {
    public string CreateToken()
    {
        // Create a random 45-byte token and encode it in a URL-safe format.
        return Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(45));
    }
  }
}
