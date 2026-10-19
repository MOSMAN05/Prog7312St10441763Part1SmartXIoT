# Smart-X IoT
### Process and control of IoT Devices

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square)
![C#](https://img.shields.io/badge/C%23-Programming-239120?style=flat-square)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat-square)

## Project Information

| Field | Details |
|---|---|
| Student | Muhammad Osman |
| Student Number | ST10441763 |
| Module | PROG7312 |
| Assessment | POE Part 2 |
| Framework | .NET 10 |
| Development Tool | Visual Studio |

## About The Project

This project uses C# along with ASP.Net Core WebAPI and Blazor Webassembly to create an IoT based project. Smart-X IoT will allow you to process telemetry from various sensors and keep track of their status.

Part 2 will dive into more advanced data structures and algorithms to better our telemetry readings and manage our devices.

## Features

- Track and monitor sensors
- Process Telemetry Using FIFO
- Previous telemetry logs and graphs
- Find devices by name and check if they are connected
- Issue command's to actuators and undo last action.
- Give priority to certain sensor issues.
- Use our device's activity history to predict what should happen next.
- Create a network and calculate the shortest path to get to a node.

## File Structure

```text
SmartXIoT/
│
├── SmartX.Api/
│ ├── Controllers/
│ └── Services/
│
├── SmartX.Client/
│ ├── Pages/
│ └── Layout/
│
├── SmartX.Shared/
│
├── SmartXIoT.slnx
└── README.md
```

We have our Api that will house most of our code and our frontend that will render all of our UI's. We will use our Shared folder for models used by both applications.

## Data Structures & Algorithms Used

| Structure | Why its being used |
|---|---|
| Queue | Allow us to process Telemetry in a fifo fashion |
| PriorityQueue | Will allow us to process more important issues first. |
| Stack | Keep a list of our previous commands to allow us to undo actions. |
| Dictionary | Can be used to search for sensors we need faster. |
| Sorted Dictionary | Will give us a sorted list of all sensor readings. |
| Sorted Set | Used to search through a time stamp range. |
| Hashset | Used to see if the active disconnected sensors already have a warning. |
| Weighted Adjacency List | Used to track what commands follow each other. |
| Dijsktra's algorithm | Will allow us to find the shortest path to our end goal. |

These data structures can allow us to optimise our search for information and reduce the number of times we search through arrays.

## Running The Application

1. Fork or clone the github repo
2. Open SmartXIoT.slnx file
3. Make sure you have .Net 10 installed.
4. Install any required Nugets.
5. Open your Solution Startup Projects.
6. Have both SmartX.Api and SmartX.Client set to start.
7. Rebuild your solution and hit F5.

#### Ip Address

- Your dashboard url should be at http://localhost:5157

- Your back end url should be at http://localhost:5023

Make sure you have both started before trying to use your app.

## Testing

#### FIFO Queue

- First send 3 requests to /api/telemetry/ingest?processImmediately=false.
- Then navigate to your /api/telemetry/queue you should see 3 packets ready to process.
- Once done send a post request to /api/telemetry/process.
- You should have processed your packets in order and now have an empty queue.

#### Sorted Lists Of Telemetry

- Navigate to your sensor telemetry tab.
- Generate some new telemetry readings.
- Update your time range of historical data you would like to see.
- Your telemetry should be updated with the new timestamps in order and graph should show temp changes.

#### Priority Queue

- Navigate to priority alerts.
- Add Normal, High and Critical alerts.
- Dispatch your alerts one by one.
- You should have processed your most critical alerts first then high and normal.

#### Predicting What To Do Next

- Navigate to your predictive tab.
- Fill your history with sample data.
- Select Inspect Moisture.
- Click on Predict next action.
- Should return with Turn Pump On as your next predicted command.
- If you try to predict something that your model hasn't learned you will receive NO_PREDICTION.

#### Network Toplogy

- Navigate to network toplogy.
- Select Hydroponics Bay.
- Click calculate shortest path.
- Your map should return with your shortest path highlighted and latency time should be 12ms.

#### Sending Command's to Actuators

- Navigate to Real Time commands.
- Reconnect the Pump Controller.
- Toggle your pump on and off.
- Select Undo last command.
- You should revert back to how it was previously and see your updated history.

## Limitations

This project only has simulated data we don't have actual IoT devices hooked up. All readings and latency times will be simulated. Our application also only has data while it is running.

## Youtube Video

YouTube Link: 

This video will demonstrate our ability to process our readings and use more advance data structures.

## Use Of AI

Will be supplied in another document with supporting evidence.

## Github Link

https://github.com/MOSMAN05/Prog7312St10441763Part1SmartXIoT


