# Google Timeline Visualizer
This is a tool that can visualize data from Google Maps Timeline on a calendar.  
Given your location history data, and a list of places, it will show a calendar with each day being colored with the places you've been in during that day:

![image](https://github.com/steveRoll-git/timeline-visualizer/assets/40004112/61a0344a-f3bc-425a-b938-879ca722c110)

## How to use
1. Download your location history data from [Google Takeout](https://takeout.google.com/) in JSON format.  
   (You can download other data if you wish, but the data that this program reads is under "Location History (Timeline)")
2. In the program, select "Data > Load JSON" and load the Records.json file.
3. Add any places you want to see visualized in "Data > Edit places".
   - A place is defined by its latitude, longitude and radius in meters.
