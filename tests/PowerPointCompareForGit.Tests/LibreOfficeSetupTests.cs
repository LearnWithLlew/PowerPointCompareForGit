namespace PowerPointCompareForGit.Tests;

public class LibreOfficeSetupTests
{
    private static readonly Installer Brew = new("brew", "brew", ["install", "--cask", "libreoffice"]);

    [Fact]
    public void FoundNeedsNoPrompt()
    {
        var prompt = new StringWriter();

        var soffice = LibreOfficeSetup.Resolve(() => "/bin/soffice", Brew, new StringReader(""), prompt, false, _ => Assert.Fail("Should not install"));

        Assert.Equal("/bin/soffice", soffice);
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void ChoosingInstallRunsInstallerAndFindsAgain()
    {
        string? installed = null;
        Installer? ran = null;

        var soffice = LibreOfficeSetup.Resolve(() => installed, Brew, new StringReader("1\n"), new StringWriter(), false, installer =>
        {
            ran = installer;
            installed = "/new/soffice";
        });

        Assert.Equal(Brew, ran);
        Assert.Equal("/new/soffice", soffice);
    }

    [Fact]
    public void PromptShowsInstallCommand()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(() => LibreOfficeSetup.Resolve(() => null, Brew, new StringReader("2\n"), prompt, false, _ => { }));

        Assert.Contains("1. Install via brew (brew install --cask libreoffice)", prompt.ToString());
    }

    [Fact]
    public void CancelDoesNotInstall()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => LibreOfficeSetup.Resolve(() => null, Brew, new StringReader("2\n"), new StringWriter(), false, _ => Assert.Fail("Should not install")));

        Assert.StartsWith("Cancelled.", error.Message);
    }

    [Fact]
    public void PipedInputFailsWithoutPrompting()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(
            () => LibreOfficeSetup.Resolve(() => null, Brew, new StringReader("1\n"), prompt, true, _ => Assert.Fail("Should not install")));
        Assert.Equal("", prompt.ToString());
    }

    [Fact]
    public void NoInstallerFailsWithoutPrompting()
    {
        var prompt = new StringWriter();

        Assert.Throws<InvalidOperationException>(
            () => LibreOfficeSetup.Resolve(() => null, null, new StringReader("1\n"), prompt, false, _ => Assert.Fail("Should not install")));
        Assert.Equal("", prompt.ToString());
    }
}
