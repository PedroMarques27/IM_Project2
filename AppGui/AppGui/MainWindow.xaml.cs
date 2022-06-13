﻿using System;
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
    public partial class MainWindow
    {
        private MmiCommunication mmiC;

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
            float confidence = float.Parse(json.recognized[2].ToString());
            string command= json.recognized[1].ToString();
            int commandId = int.Parse(json.recognized[0].ToString());
            Console.WriteLine(commandId);

            if (webDriver.FindElements(By.XPath("//div[@class='config-warning-popover']//button")).Count() > 0)
            { 
                webDriver.FindElement(By.XPath("//div[@class='config-warning-popover']//button")).Click();
            }
            switch (commandId)
            {
                case 0: // OPTIONS - both arms up
                    try
                    {
                        if (webDriver.FindElements(By.XPath("//div[@class='top-buttons ']//button[@class='top-buttons-button options ']")).Count() > 0)
                        {
                            webDriver.FindElement(By.XPath("//div[@class='top-buttons ']//button[@class='top-buttons-button options ']")).Click();
                        }
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

                case 1: // PAUSE - clap
                    if (webDriver.FindElements(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button not-paused']")).Count() > 0)
                    {
                        webDriver.FindElement(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button not-paused']")).Click();
                    }

                    if (webDriver.FindElements(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button paused']")).Count() > 0)
                    {
                        webDriver.FindElement(By.XPath("//button[@class='button-1 dark-gray small-button pause-game-button paused']")).Click();
                    }

                    break;


                case 2: // RAISE BET VALUE - open hand
                    try
                    {
                        try
                        {
                            string current = webDriver.FindElement(By.XPath("//div[@class='raise-bet-value ']//div[@class='value-input-ctn']//input")).GetAttribute("value");
                            string money = webDriver.FindElement(By.XPath("//p[@class='blind-value']//span[@class='normal-value']")).GetAttribute("innerHTML");
                            int value = int.Parse(current) + int.Parse(money);

                            IWebElement element = webDriver.FindElement(By.XPath("//div[@class='value-input-ctn']//input"));
                            element.Click();
                            element.Clear();
                            element.SendKeys(value.ToString());
                        }
                        catch { }
                    }
                    catch { }
                    break;

                case 3: // RAISE - arm up
                    try
                    {
                        try
                        {
                            webDriver.FindElement(By.XPath("//div[@class='action-buttons game-decisions']//button[@class='button-1 with-tip raise green']")).Click();
                        }
                        catch { }
                    }
                    catch { }
                    break;

                case 4: // FOLD - stomp leg
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

                case 5: // CHECK - horizontal arm swipe
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

                case 6: // BET - thumbs up
                    try
                    {
                        try
                        {
                            webDriver.FindElement(By.XPath("//div[@class='action-buttons game-decisions']//button[@class='button-1 call with-tip call green ']")).Click();
                        }
                        catch { }
                    }
                    catch { }
                    break;
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
