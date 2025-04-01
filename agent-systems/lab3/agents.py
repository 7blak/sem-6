from mesa import Agent, Model

class PersonAgent(Agent):
    def __init__(self, model: Model,
                 sport_enthusiast: bool=True,
                 knows_about_trend: bool=False,
                 gossip_probability: float=0.5):
        super().__init__(model)

        self.sport_enthusiast = sport_enthusiast
        self.knows_about_trend = knows_about_trend
        self.gossip_probability = gossip_probability

    def introduce_self(self):
        print(f"Hello, I am {self.unique_id}.")

    def move_around(self):
        print(f'{self.unique_id} is moving around (position: {self.pos}).')

        possible_movement = self.model.grid.get_neighborhood(self.pos, moore=True, include_center=False)
        new_position = self.random.choice(possible_movement)
        self.model.grid.move_agent(self, new_position)
    
    def spread_trend(self):
        if not self.knows_about_trend:
            print(f"[{self.unique_id}] I don't know about the trend yet.")
            return
        
        encountered_agents = self.model.grid.get_cell_list_contents([self.pos])
        encountered_agents.pop(encountered_agents.index(self))

        for agent in encountered_agents:
            if agent.sport_enthusiast or self.random.random() < self.gossip_probability:
                agent.learn_about_trend()
                print(f"[{self.unique_id}] I told {agent.unique_id} about the trend.")
    
    def learn_about_trend(self):
        self.knows_about_trend = True