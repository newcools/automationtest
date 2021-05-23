using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;

namespace SbsUiAutomationDemo
{
    public class SbsQuickTest
    {
        private IWebDriver driver;

        [SetUp]
        public void startBrowser()
        {
            driver = new ChromeDriver();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        [Test]
        // TODO: run different browser size for different test coverage
        // for example, driver.Manage().Window.Maximize();
        public void QuickBrutalOne()
        {
            try
            {
                driver.Url = @"https://www.sbs.com.au/language/greek/audio/cyprus-will-not-stop-the-use-of-the-astrazeneca-vaccine";

                // Navigate to this url and verify the title of the audio track
                var titleElement = driver.FindElement(By.ClassName("audiotrack__title"));
                Assert.AreEqual(@"Δεν αποσύρει το εμβόλιο της AstraZeneca η Κύπρος", titleElement.Text);

                // Verify Subscribe dropdown displays apple and google podcasts
                var subscriberDropdownButton = driver.FindElement(By.CssSelector("div.audiotrack__action--subscribe div.podcast-subscribe__label.dropdown__button"));
                subscriberDropdownButton.Click();


                var subscribeSelectList = driver.FindElements(By.CssSelector("ul.podcast-subscribe__list li.podcast-subscribe__item"));
                var applePodcastsOption = subscribeSelectList.FirstOrDefault(i => i.Text.ToLower() == "apple podcasts");
                var googlePodcastsOption = subscribeSelectList.FirstOrDefault(i => i.Text.ToLower() == "google podcasts");

                Assert.IsNotNull(applePodcastsOption);
                Assert.IsNotNull(googlePodcastsOption);

                // ** more robust this way as some webelement may render slower than you expected.
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                var visibleApplePodcastsOption = wait.Until(d =>
                {
                    if (applePodcastsOption.Displayed)
                        return applePodcastsOption;
                    return null;
                });
                var visibleGooglePodcastsOption = wait.Until(d =>
                {
                    if (googlePodcastsOption.Displayed)
                        return googlePodcastsOption;
                    return null;
                }
                );

                Assert.IsNotNull(visibleApplePodcastsOption);
                Assert.IsNotNull(visibleGooglePodcastsOption);

                // Click Play on the audio icon and verify audio player is launched at the bottom of the screen
                // ** the player UI is responsive , need to be cautious here 
                // ** apparently the play button is there regardless of the view port size
                var playButtonElememnt = driver.FindElement(By.CssSelector("button.audiotrack__button.audiotrack__button--play-pause"));
                playButtonElememnt.Click();

                var playerPanel = wait.Until(d =>
                {
                    var pp = d.FindElement(By.CssSelector(".audio-player"));
                    if (pp.Displayed)
                        return pp;
                    return null;
                });

                Assert.IsNotNull(playerPanel);

                // Click and verify player controls – Play and pause, mute and unmute                           
                IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                var audioPaused = jse.ExecuteScript("return document.querySelector('audio.audio-player__media').paused;");
                Assert.AreEqual("false", audioPaused.ToString().ToLower());

                // click play button again, this time we use the play button in the player control
                playButtonElememnt = driver.FindElement(By.CssSelector("button.audio-player__button--play-pause"));
                playButtonElememnt.Click();
                audioPaused = jse.ExecuteScript("return document.querySelector('audio.audio-player__media').paused;");
                Assert.AreEqual("true", audioPaused.ToString().ToLower());

                // ** mute unmute button
                var muteUnmuteButton = driver.FindElement(By.CssSelector("button.audio-player__volume-button"));
                // ** due to responsive design the mute button may be invisible
                if (!muteUnmuteButton.Displayed)
                {
                    driver.FindElement(By.CssSelector("div.audio-player__trackinfo--toggle")).Click();
                    wait.Until(d =>
                    {
                        var mub = d.FindElement(By.CssSelector("button.audio-player__volume-button"));
                        if (mub.Displayed)
                            return mub;
                        return null;
                    });
                }

                muteUnmuteButton.Click();
                var audioMuted = jse.ExecuteScript("return document.querySelector('audio.audio-player__media').muted;");
                Assert.AreEqual("true", audioMuted.ToString().ToLower());

                muteUnmuteButton.Click();
                audioMuted = jse.ExecuteScript("return document.querySelector('audio.audio-player__media').muted;");
                Assert.AreEqual("false", audioMuted.ToString().ToLower());

                // Click 20s forward or rewind on the audio player and verify scrub on the progress bar
                // ** due to the limited time not handling buffering and start/end scenario but can be improved
                
                // TODO: improve by moving the progress bar to different locations
                var forwardButton = driver.FindElement(By.CssSelector("button.audio-player__button--step-forward"));
                var backwardButton = driver.FindElement(By.CssSelector("button.audio-player__button--step-back"));
                var audioDurationElement = driver.FindElement(By.CssSelector("span.audio-player__time--duration"));
                var audioElapsedTimeElement = driver.FindElement(By.CssSelector("span.audio-player__time--elapsed"));

                // TODO
                // ** this need a refactor as the audio/video length can be hours, this can cause problems,
                // ** for now leave it as it , but must fix it later
                string timeFormat = @"mm\:ss";
                TimeSpan totalDuration = TimeSpan.ParseExact(audioDurationElement.Text, timeFormat, null);
                TimeSpan beforeElapsedTime = TimeSpan.ParseExact(audioElapsedTimeElement.Text, timeFormat, null);

                // ** forward
                forwardButton.Click();
                TimeSpan afterElapsedTime = TimeSpan.ParseExact(audioElapsedTimeElement.Text, timeFormat, null);
                var expectedTime = beforeElapsedTime.Add(TimeSpan.FromSeconds(20));
                if (expectedTime > totalDuration) expectedTime = totalDuration;
                Assert.AreEqual(expectedTime, afterElapsedTime);

                // ** backward
                backwardButton.Click();
                afterElapsedTime = TimeSpan.ParseExact(audioElapsedTimeElement.Text, timeFormat, null);
                expectedTime = beforeElapsedTime.Subtract(TimeSpan.FromSeconds(20));
                if (expectedTime < TimeSpan.Zero) expectedTime = TimeSpan.Zero;
                Assert.AreEqual(expectedTime, afterElapsedTime);

                // Verify clicking on language toggle(top right corner of the page) displays language list
                var languageToggleElement = driver.FindElement(By.CssSelector("a.global-nav__language-toggle"));
                Actions action = new Actions(driver);
                action.MoveToElement(languageToggleElement).Perform();
                var languageOptions = wait.Until(d =>
                {
                    var lo = d.FindElement(By.CssSelector("div.dropdown__body"));
                    if (lo.Displayed)
                        return lo;
                    return null;
                });
                Assert.IsNotNull(languageOptions);
                Console.WriteLine(languageOptions.GetAttribute("innerText"));
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TearDown]
        public void closeBrowser()
        {
            driver.Close();
        }

    }
}