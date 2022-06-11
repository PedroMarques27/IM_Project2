using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using mmisharp;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System.IO;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Events;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace AppGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private MmiCommunication mmiC;

        //  new 16 april 2020
        private MmiCommunication mmiSender;
        private LifeCycleEvents lce;
        private MmiCommunication mmic;
        private IWebDriver webDriver;
        private String banco;

        public MainWindow()
        {
            //Creates the ChomeDriver object, Executes tests on Google Chrome
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;

            //Creates the ChomeDriver object, Executes tests on Google Chrome

            webDriver = new ChromeDriver(path + @"/driver/");
            webDriver.Navigate().GoToUrl("https://www.pokernow.club/start-game");

            mmiC = new MmiCommunication("localhost",8000, "User1", "GUI");
            mmiC.Message += MmiC_Message;
            mmiC.Start();

            //init LifeCycleEvents..
            lce = new LifeCycleEvents("APP", "TTS", "User1", "na", "command"); // LifeCycleEvents(string source, string target, string id, string medium, string mode
            // MmiCommunication(string IMhost, int portIM, string UserOD, string thisModalityName)
            mmic = new MmiCommunication("localhost", 8000, "User1", "GUI");
        }

        private void MmiC_Message(object sender, MmiEventArgs e)
        {
            Console.WriteLine(e.Message);
            var doc = XDocument.Parse(e.Message);
            var com = doc.Descendants("command").FirstOrDefault().Value;
            dynamic json = JsonConvert.DeserializeObject(com);
            
            string[] repeat = { "Desculpe, não percebi, pode repetir?", "Não o consegui ouvir, pode repetir por favor?", "Poderia repetir se faz favor? Não percebi bem" };
            Random r = new Random();
            float confidence = float.Parse(json.confidence[0].ToString());
            if(0.35<confidence && confidence<0.75)
                sendMessageToTts(repeat[r.Next(0, 2)]);

            else
            {
                if (webDriver.FindElements(By.XPath("//div[@class='config-warning-popover']//button")).Count() > 0)
                    webDriver.FindElement(By.XPath("//div[@class='config-warning-popover']//button")).Click();
                switch ((String)json.recognized[0].ToString().Split(':')[0])
                {
                    case "START":
                        if (webDriver.FindElements(By.XPath("//div[@class='out-game-decisions action-buttons']")).Count() > 0)
                            webDriver.FindElement(By.XPath("//div[@class='out-game-decisions action-buttons']//button[@class='button-1 green highlighted']")).Click();

                        break;

                    case "SIT":
                        String newbanco= ((String)json.recognized[0].ToString().Split(':')[1]).ToString();
                        String dinheiro = ((String)json.recognized[0].ToString().Split(':')[2]).ToString();
                        if (int.Parse(newbanco) > 10)
                        {
                            sendMessageToTts("Peço desculpa, esse banco não existe, escolha um entre 1 e 10");
                        }
                        else
                        {
                            try
                            {
                                webDriver.FindElement(By.XPath("//div[@class='table-player table-player-seat table-player-" + newbanco + "']/button")).Click();
                                webDriver.FindElement(By.XPath("//input[@placeholder='Your Stack']")).SendKeys(dinheiro);
                                IWebElement buttonText = webDriver.FindElement(By.XPath("//button[@class='button-1 med-button highlighted green']"));
                                Console.WriteLine(buttonText.GetAttribute("innerHTML"));
                                buttonText.Click();
                                banco = newbanco;
                                sendMessageToTts("Sentado no banco " + newbanco + " com " + dinheiro + " euros");
                            }
                            catch
                            {
                                sendMessageToTts("Já estás sentado no banco "+banco);
                            }
                        }
                        break;

                    case "VOICECHAT":
                        if (webDriver.FindElements(By.XPath("//div[@class='conf-controls']//button")).Count() > 0)
                        {
                            switch (((String)json.recognized[0].ToString().Split(':')[1]).ToString())
                            {
                                case "0":
                                    foreach (IWebElement elem in webDriver.FindElements(By.XPath("//div[@class='conf-controls']//button")))
                                    {
                                        if (elem.GetAttribute("class").Contains("active"))
                                        {
                                            elem.Click();
                                        }
                                    }
                                    break;
                                case "1":
                                    var turnedOn = 0;
                                    foreach (IWebElement elem in webDriver.FindElements(By.XPath("//div[@class='conf-controls']//button")))
                                    {
                                        if (!elem.GetAttribute("class").Contains("active"))
                                        {
                                            elem.Click();
                                            turnedOn++;
                                        }
                                    }
                                    if (turnedOn > 0)
                                        sendMessageToTts("Atenção, a câmara está a ligar!");
                                    break;
                            }
                        }
                        break;

                    case "END":
                        if (webDriver.FindElements(By.XPath("//button[@class='stop-game-control-button-container with-tip-top-right-button-ctn']")).Count() > 0)
                        {
                            webDriver.FindElement(By.XPath("//button[@class='stop-game-control-button-container with-tip-top-right-button-ctn']")).Click();
                            sendMessageToTts("Jogo terminará na próxima ronda");
                        }
                        break;

                    case "OPTIONS":

                        try
                        {
                            if (webDriver.FindElements(By.XPath("//div[@class='top-buttons ']//button[@class='top-buttons-button options ']")).Count() > 0)
                                webDriver.FindElement(By.XPath("//div[@class='top-buttons ']//button[@class='top-buttons-button options ']")).Click();
                        }
                        catch { }

                        try
                        {
                            IList<IWebElement> webElements = webDriver.FindElements(By.XPath("//div[@class='config-top-tabs']//button"));
                            webElements[2].Click();
                            break;
                        }
                        catch { }
                        break;

                    case "PAUSE":
                        if (webDriver.FindElements(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button not-paused']")).Count() > 0)
                            webDriver.FindElement(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button not-paused']")).Click();
                        break;

                    case "PLAYERS":
                        try
                        {
                            if (webDriver.FindElements(By.XPath("//div[@class='top-buttons ']//button[@class='top-buttons-button options ']")).Count() > 0)
                                webDriver.FindElement(By.XPath("//div[@class='top-buttons ']//button[@class='top-buttons-button options ']")).Click();
                        }
                        catch { }

                        try
                        {
                            IList<IWebElement> webElements = webDriver.FindElements(By.XPath("//div[@class='config-top-tabs']//button"));
                            webElements[1].Click();
                            break;
                        }
                        catch { }
                        break;

                    case "UNPAUSE":
                        if (webDriver.FindElements(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button paused']")).Count() > 0)
                            webDriver.FindElement(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button paused']")).Click();
                        break;

                    case "RAISE":
                        try
                        {
                            try
                            {
                                String dinheir = ((String)json.recognized[0].ToString().Split(':')[1]).ToString();
                                Console.WriteLine(" dinheiro:          " + dinheir);
                                webDriver.FindElement(By.XPath("//div[@class='action-buttons game-decisions']//button[@class='button-1 with-tip raise green']")).Click();
                                sendMessageToTts("vou apostar");
                                webDriver.FindElement(By.XPath("//div[@class='value-input-ctn']//input")).SendKeys(dinheir);
                                sendMessageToTts("apostar" + dinheir + "euros");
                                webDriver.FindElement(By.XPath("//div[@class='action-buttons']//input[@class='button-1 green bet']")).Click();
                                sendMessageToTts("Aposta feita");
                            }
                            catch { }
                        }
                        catch { }
                        break;

                    case "FOLD":
                        try
                        {
                            try
                            {
                                webDriver.FindElement(By.XPath("//div[@class='action-buttons game-decisions']//button[@class='button-1 with-tip fold red ']")).Click();
                                sendMessageToTts("O jogador desistiu");
                            }
                            catch { }
                        }
                        catch { }
                        break;

                    case "CHECK":
                        try
                        {
                            try
                            {
                                webDriver.FindElement(By.XPath("//div[@class='action-buttons game-decisions']//button[@class='button-1 with-tip check green ']")).Click();
                                sendMessageToTts("O jogador passou");
                            }
                            catch { }
                        }
                        catch { }
                        break;

                    case "BET":
                        try
                        {
                            try
                            {
                                webDriver.FindElement(By.XPath("//div[@class='action-buttons game-decisions']//button[@class='button-1 call with-tip call green ']")).Click();
                                sendMessageToTts("O jogador igualou");
                            }
                            catch { }
                        }
                        catch { }
                        break;
                }
            }
        }
       
        public void sendMessageToTts(String s)
        {
            mmic.Send(lce.NewContextRequest());

            string json2 = "";
            json2 += s;
            var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, json2);
            mmic.Send(exNot);            
        }

     
    }
}
