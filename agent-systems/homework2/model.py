import logging

from mesa import Model
from mesa.experimental.cell_space import OrthogonalVonNeumannGrid

from agents import PersonAgent

logger = logging.getLogger(__name__)

class InfectiousDiseaseSpreadModel(Model):
    population_size: int
    infected_population_size: int
    comorbidities_population_size: int
    moving_probability: float
    grid: OrthogonalVonNeumannGrid
    seed: int

    def __init__(self,
                 population_size: int = 10,
                 infected_population_size: int = 1,
                 comorbidities_population_size: int = 4,
                 moving_probability: float = 0.5,
                 grid_width: int = 10,
                 grid_height: int = 10,
                 seed: int = None):
        super().__init__(seed = seed)

        self.population_size = population_size
        self.infected_population_size = infected_population_size
        self.comorbidities_population_size = comorbidities_population_size
        self.moving_probability = moving_probability

        self.grid = OrthogonalVonNeumannGrid((grid_width, grid_height), True, random=self.random)

        self.generate_person_agents()
        
    def generate_person_agents(self):
        PersonAgent.create_agents(self, self.population_size - self.infected_population_size - self.comorbidities_population_size, 
                                 is_infected = False,
                                 has_comorbidities = False, 
                                 moving_probability = self.moving_probability)
        PersonAgent.create_agents(self, self.infected_population_size,
                                 is_infected = True,
                                 has_comorbidities = False, 
                                 moving_probability = self.moving_probability)
        PersonAgent.create_agents(self, self.comorbidities_population_size,
                                 is_infected = False,
                                 has_comorbidities = True, 
                                 moving_probability = self.moving_probability)

        self.place_agents_on_grid()
    
    def place_agents_on_grid(self):
        x_range = self.rng.integers(0, self.grid.width, size=self.population_size)
        y_range = self.rng.integers(0, self.grid.height, size=self.population_size)

        for agent, x_coord, y_coord in zip(self._agents_by_type[PersonAgent], x_range, y_range):
            agent.cell = self.grid[(x_coord, y_coord)]

    def _stop_condition(step) -> None:
        def perform_step(self):
            if len(self._agents_by_type[PersonAgent].select(lambda agent: not agent.is_infected)) == 0:
                self.running = False
                logger.info("All agents are infected. Stopping the model.")
            else:
                step(self)
            
        return perform_step
    
    @_stop_condition
    def step(self):
        self._agents_by_type[PersonAgent].do("move_around")
        self._agents_by_type[PersonAgent].do("infect_others")
