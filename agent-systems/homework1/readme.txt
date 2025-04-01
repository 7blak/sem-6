Hello! This readme will serve as a brief overview of the project.

- In order to run a sample test, type in a single digit (1, 2, 3, 4 or 5) and press enter. The code for what agents are executed can be found in Engine.java

// Why are all of the Clients always choosing the same DeliveryAgent?
- Currently in my implementation each DeliveryAgent has access to all Markets, which basically means the offers (should! this is good for debugging) differ only by the delivery fee. As this was not specified exactly I left it as is.

- The program supports multiple agents, I have left a few methods to test on. Technically seems to be no limit (unless JADE messes up - that is out of my control).

- The 100 ClientAgent-DeliveryAgent-MarketAgent executes in about 2 minutes on a i5-10600K @ 4.10Ghz.

- The project was turned late because I really wanted it to function with multiple agents and so I spent hours trying to work out an efficient and elegant solution - I believe I did well!