using NUnit.Framework;

public class TestResolveUrl
{
    private const string BaseUrl = "https://raw.githubusercontent.com/usuario/repo/main/images";

    private static string ResolveUrl(string url, string baseUrl)
    {
        return url.StartsWith("http")
            ? url
            : $"{baseUrl.TrimEnd('/')}/{url}";
    }

    [Test]
    public void UrlRelativa_SeResuelveConBase()
    {
        string result = ResolveUrl("imagen.png", BaseUrl);
        Assert.AreEqual(BaseUrl + "/imagen.png", result);
    }

    [Test]
    public void UrlAbsoluta_NoSeModifica()
    {
        string url = "https://otro.com/imagen.png";
        string result = ResolveUrl(url, BaseUrl);
        Assert.AreEqual(url, result);
    }
}
