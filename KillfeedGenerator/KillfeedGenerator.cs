using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;

namespace HLTVDiscordBridge.KillfeedGenerator
{
    public class KillfeedGenerator
    {
        private Font font;

        public KillfeedGenerator()
        {
            font = new Font("res/stratum2-bold-webfont.ttf");
        }

        /// <summary>
        /// Generates a CS:GO Killfeed Image from given parameters and saves it as "killfeed.png" into the current working directory.
        /// </summary>
        public void GenerateImage(string firstPlayerName, bool isTerrorist)
        {
            Text text = new Text(firstPlayerName, font, 40);

            if(isTerrorist)
            {
                text.FillColor = new Color(0xC7, 0xA2, 0x47);
            } else
            {
                text.FillColor = new Color(0x4F, 0x9E, 0xDE);
            }

            text.Position = new Vector2f(5.0f, 5.0f);

            RenderTexture renderTex = new RenderTexture((uint)text.GetLocalBounds().Width+10, (uint)text.GetLocalBounds().Height+10);

            renderTex.Clear(new Color(55, 55, 55));
            renderTex.Draw(text);
            renderTex.Display();

            // Save the RenderTexture as a .png file
            renderTex.Texture.CopyToImage().SaveToFile("killfeed.png");
        }   
    }
}
