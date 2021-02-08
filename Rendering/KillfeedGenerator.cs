using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;

namespace HLTVDiscordBridge.Rendering
{
    public class KillfeedGenerator
    {
        private Font font;
        private const float fontSize = 32;

        private Color terroristColor;
        private Color counterTerroristColor;

        private IconRegistry iconRegistry;

        private Dictionary<string, Texture> weaponTextures;
        private Texture headshotTexture;

        private RectangleShape weaponShape;
        private RectangleShape headshotShape;

        public KillfeedGenerator()
        {
            font = new Font("res/stratum2-bold-webfont.ttf");

            terroristColor = new Color(0xC7, 0xA2, 0x47);
            counterTerroristColor = new Color(0x4F, 0x9E, 0xDE);

            iconRegistry = new IconRegistry();

            weaponTextures = new Dictionary<string, Texture>();

            foreach(string iconName in iconRegistry.GetIconNames("res/icons/weapons"))
            {
                weaponTextures.Add(iconName, new Texture("res/icons/weapons/" + iconName + ".png"));
            }

            headshotTexture = new Texture("res/headshot.png");

            headshotShape = new RectangleShape(new Vector2f(fontSize, fontSize));
            headshotShape.Texture = headshotTexture;
        }

        /// <summary>
        /// Generates a CS:GO Killfeed Image from given parameters and saves it as "killfeed.png" into the current working directory.
        /// </summary>
        public void GenerateImage(string firstPlayerName, string secondPlayerName, bool firstPlayerIsTerrorist, bool secondPlayerIsTerrorist, string weapon, bool isHeadshot)
        {


            Text firstPlayerText = new Text(firstPlayerName, font, (uint)fontSize);
            Text secondPlayerText = new Text(secondPlayerName, font, (uint)fontSize);

            firstPlayerText.FillColor   =   (firstPlayerIsTerrorist)    ? terroristColor : counterTerroristColor;
            secondPlayerText.FillColor  =   (secondPlayerIsTerrorist)   ? terroristColor : counterTerroristColor;

            firstPlayerText.Position = new Vector2f(fontSize / 4, 0.0f);
            secondPlayerText.Position = new Vector2f(firstPlayerText.GetGlobalBounds().Width + fontSize / 4.0f + fontSize * 0.42f, 0.0f);

            uint renderTexWidth = (uint)(firstPlayerText.GetGlobalBounds().Width + secondPlayerText.GetGlobalBounds().Width + (fontSize * 0.42f) /*<-- Inner Margin*/ + (fontSize * 2.0f/3.0f)) /*<-- Space between PlayerTexts*/;
            uint renderTexHeight = (uint)(Math.Max(Math.Max(fontSize, firstPlayerText.GetGlobalBounds().Height), secondPlayerText.GetGlobalBounds().Height) + (fontSize * 0.42f));

            weaponShape = new RectangleShape(new Vector2f(fontSize, fontSize));

            bool drawWeapon = weaponTextures.ContainsKey(weapon);
            if(drawWeapon)
            {
                weaponShape = new RectangleShape(new Vector2f(fontSize * ((float)weaponTextures[weapon].Size.X / (float)weaponTextures[weapon].Size.Y), fontSize));
                weaponShape.Texture = weaponTextures[weapon];

                // Offset second player text if weapon texture is registered
                renderTexWidth += (uint)(weaponShape.Size.X + (fontSize * 0.42f / 2.0f));
                weaponShape.Position = new Vector2f(secondPlayerText.Position.X, secondPlayerText.Position.Y + (fontSize * 0.21f));
                secondPlayerText.Position = new Vector2f(secondPlayerText.Position.X + (uint)weaponShape.Size.X + (fontSize * 0.42f / 2.0f), secondPlayerText.Position.Y);
            } else
            {
                throw new NotImplementedException($"Missing weapon: {weapon}");
            }

            if(isHeadshot)
            {
                renderTexWidth += (uint)(headshotShape.Size.X + (fontSize * 0.42f / 2.0f));
                headshotShape.Position = new Vector2f(secondPlayerText.Position.X, secondPlayerText.Position.Y + 2*(fontSize / 8.0f));
                secondPlayerText.Position = new Vector2f(secondPlayerText.Position.X + (uint)headshotShape.Size.X + (fontSize * 0.42f / 2.0f), secondPlayerText.Position.Y);
            }

            RenderTexture renderTex = new RenderTexture(renderTexWidth, renderTexHeight);
            renderTex.SetActive(true);

            //renderTex.Clear(new Color(0, 0, 0, 0));
            renderTex.Clear(new Color(0x36, 0x39, 0x3F));
            renderTex.Draw(firstPlayerText);
            renderTex.Draw(weaponShape);
            if (isHeadshot) renderTex.Draw(headshotShape);
            renderTex.Draw(secondPlayerText);
            renderTex.Display();

            // Save the RenderTexture as a .png file
            renderTex.Texture.CopyToImage().SaveToFile("killfeed.png");
            renderTex.Dispose();
        }   
    }
}
