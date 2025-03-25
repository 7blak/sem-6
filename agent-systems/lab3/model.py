from mesa import Model
from mesa.space import MultiGrid

from agents import PersonAgent

class TrendModel(Model):

    def __init__(self, population_size: int = 5,
                    width: int = 10,
                    height: int = 10,
                    seed=None):
        super().__init__(seed=seed)

        self.grid = MultiGrid(width, height, True)
        self.population_size = population_size
        self.width = width
        self.height = height

        sport_enthusiast = self.random.randint(0, self.population_size)
        non_sport_enthusiast = self.population_size - sport_enthusiast

        PersonAgent.create_agents(self, sport_enthusiast, sport_enthusiast=True)
        PersonAgent.create_agents(self, non_sport_enthusiast, sport_enthusiast=False)

        self.register_agent(PersonAgent(self, sport_enthusiast=True, knows_about_trend=True))
        self.population_size += 1

        self.place_agents_on_grid()

    def place_agents_on_grid(self):
        x_range = self.rng.integers(0, self.width, self.population_size)
        y_range = self.rng.integers(0, self.height, self.population_size)

        for agent, x_coord, y_coord in zip(self.agents, x_range, y_range):
            self.grid.place_agent(agent, (x_coord, y_coord))

    def step(self):
        self.agents.shuffle_do('introduce_self')
        self.agents.shuffle_do('move_around')
        self.agents.do('spread_trend')

        # self.agents.select(lambda agent: isinstance(agent, PersonAgent))

        # self.agents.do('spread_trend')