﻿using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitterTraffic.Bots;

namespace TwitterTraffic
{
	public class Twitter
	{
		private readonly TwitterContext TwitterConnection;

		public async void PostTweet(string message)
		{
			try
			{
				await TwitterConnection.TweetAsync(message);
				Console.WriteLine("Updated status successfully");
			}
			catch
			{
				Console.WriteLine("Status hasn't changed");
			}
		}

		public DateTime GetLastTweet(string screenName)
		{
			Status lastTweet = TwitterConnection.Status.Where(s => s.Type == StatusType.User && s.ScreenName == screenName && s.InReplyToScreenName == null).FirstOrDefault();
			//Console.WriteLine(lastTweet.Text);
			if (lastTweet != null)
				return Utils.GetLocalTime(lastTweet.CreatedAt);

			return Utils.GetLocalTime();
		}

		public Twitter(TwitterContext twitterContext)
		{
			TwitterConnection = twitterContext;
		}

		public bool CheckLastTweetNotRecent(Timers timers, string screenName, out int sleepCounter)
		{
			sleepCounter = 0;
			bool inTimeZone;
			string timePeriod;

			Console.WriteLine("Checking status of last Tweet for {0}", screenName);
			DateTime lastTweetTime = GetLastTweet(screenName);
			DateTime currentTime = Utils.GetLocalTime();
			Console.WriteLine("Last Tweet was at: {0} for {1}", lastTweetTime.ToString("dd/MM/yyyy HH:mm:ss"),
				screenName);

			//Check current time isn't out of bounds
			if (timers.CheckOutOfHours(currentTime, out sleepCounter))
				return true;

			if (lastTweetTime.Date < currentTime.Date)
			{
				Console.WriteLine("Last Tweet is at least one day behind for {0}", screenName);
				sleepCounter = 0;
				return false;
			}

			sleepCounter = timers.CheckTimers(currentTime, out timePeriod, out inTimeZone);

			// If the date is the same, lets alignt the timer to the last Tweet time for timing accuracy
			if (lastTweetTime.Date == currentTime.Date && sleepCounter > 0)
			{
				// TODO: Check its within 15mins
				Console.WriteLine("Syncing sleep timer to last Tweet for {0}", screenName);
				int timeDiff =
					Math.Abs(Convert.ToInt32(sleepCounter - currentTime.Subtract(lastTweetTime).TotalMilliseconds));
				int overSleepCounter = timers.CheckOverShootTimer(currentTime, sleepCounter);

				if (timeDiff < overSleepCounter)
				{
					Console.WriteLine("Favouring the shorter timer for {0}", screenName);
					sleepCounter = timeDiff;
				}
				else
				{
					Console.WriteLine("Favouring the adjusted timer for {0}", screenName);
					sleepCounter = overSleepCounter;
				}
			}

			if (sleepCounter > 0)
			{
				Console.WriteLine("Last Tweet is recent for {0} - Sleeping until: {1}", screenName,
					currentTime.AddSeconds(sleepCounter / 1000).ToString("dd/MM/yyyy HH:mm:ss"));
				return true;
			}
			else
			{
				Console.WriteLine("Last Tweet is stale for {0}: {1}", screenName,
					lastTweetTime.ToString("dd/MM/yyyy HH:mm:ss"));
				return false;
			}
		}
	}
}