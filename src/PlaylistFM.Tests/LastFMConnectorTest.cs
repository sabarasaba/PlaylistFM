using PlaylistFM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace PlaylistFM.Tests
{
    [TestClass()]
    public class LastFMConnectorTest
    {
        private LastFMConnector lastfmConnector;

        [TestInitialize()]
        public void MyTestInitialize()
        {
            this.lastfmConnector = new LastFMConnector();
        }

        /// <summary>
        ///A test for getArtistTopTracks
        ///</summary>
        [TestMethod()]
        public void getArtistTopTracksTest()
        {
            string artist = "Bob Dylan";
            List<string> actual = this.lastfmConnector.getArtistTopTracks(artist);
            Assert.IsNotNull(actual);
        }

        /// <summary>
        ///A test for checking if an artist is valid.
        ///</summary>
        [TestMethod()]
        public void isValidArtistBobDylan()
        {
            string artist = "Bob Dylan";
            bool expected = true;
            bool actual;
            actual = this.lastfmConnector.isValidArtist(artist);
            Assert.AreEqual(expected, actual, "lolwut ? this shouldnt be failing.");
        }

        /// <summary>
        ///A test for checking if an artist is invalid.
        ///</summary>
        [TestMethod()]
        public void isNotValidArtistLoWuachiturro()
        {
            string artist = "Lo Wachiturro";
            bool actual = this.lastfmConnector.isValidArtist(artist);
            Assert.IsFalse(actual, "Lo wachiturro arent a valid artist.");
        }

        /// <summary>
        /// Test if the sanitize method is working.
        ///</summary>
        [TestMethod()]
        public void sanitizeSongNameTest()
        {
            string song = @"The Times They Are a-Changin'";
            LastFMConnector.sanitizeSongName(song);
            Assert.IsFalse(LastFMConnector.sanitizeSongName(song).Contains(@"'"));
            Assert.IsFalse(LastFMConnector.sanitizeSongName(song).Contains(@"-"));
        }
    }
}
