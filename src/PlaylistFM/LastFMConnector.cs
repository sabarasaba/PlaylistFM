using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PlaylistFM
{
    class LastFMConnector
    {
        public LastFMConnector() { }

        private string getArtistURI(string artist)
        {
            return @"http://ws.audioscrobbler.com/2.0/?method=artist.gettoptracks&artist=" + artist.Replace(" ", "+") + @"&api_key=b25b959554ed76058ac220b7b2e0a026";
        }

        public bool isValidArtist(string artist)
        {
            try
            {
                string uri = this.getArtistURI(artist);
                XDocument document = XDocument.Load(uri);
            }
            catch(System.Exception)
            {
                return false;
            }

            return true;
        }

        public static string sanitizeSongName(string song)
        {
            char[] v = song.ToCharArray();

            v = Array.FindAll<char>(v, (c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))));

            return new string(v).ToLower();
        }

        public List<string> getArtistTopTracks(string artist)
        {
            List<string> trackNames = new List<string>();
            string uri = this.getArtistURI(artist);
            XDocument document = XDocument.Load(uri);

            foreach (XElement xe in document.Descendants("track"))
            {
                trackNames.Add(sanitizeSongName(xe.Element("name").Value));
            }

            return trackNames;
        }

    }
}
