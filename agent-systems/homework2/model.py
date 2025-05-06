import logging
import numpy as np

from mesa import Model
from mesa.experimental.cell_space import OrthogonalVonNeumannGrid, PropertyLayer
from mesa.datacollection import DataCollector

from agents import PersonAgent

logger = logging.getLogger(__name__)

class InfectiousDiseaseSpreadModel(Model):
    population_size: int
    infected_population_size: int
    comorbidities_population_size: int
    infected_cells_count: int
    moving_probability: float
    datacollector: DataCollector
    direct_interactions_count: int = 0
    location_interactions_count: int = 0
    grid: OrthogonalVonNeumannGrid
    seed: int


    def __init__(self,
                 population_size: int = 10,
                 infected_population_size: int = 1,
                 comorbidities_population_size: int = 4,
                 infected_cells_count: int = 1,
                 moving_probability: float = 0.5,
                 grid_width: int = 10,
                 grid_height: int = 10,
                 seed: int = None):
        super().__init__(seed = seed)

        self.population_size = population_size
        self.infected_population_size = infected_population_size
        self.comorbidities_population_size = comorbidities_population_size
        self.infected_cells_count = infected_cells_count
        self.moving_probability = moving_probability

        self.grid = OrthogonalVonNeumannGrid((grid_width, grid_height), True, random=self.random)

        self.generate_person_agents()
        self.generate_infected_cell_locations(grid_width, grid_height, infected_cells_count)
        self.init_data_collector()
        

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


    def generate_infected_cell_locations(self, grid_width: int, grid_height: int, infected_cells_count: int):
        infected_cell_locations = self.get_random_infected_cell_distribution(grid_width, grid_height, infected_cells_count)

        for cell in self.grid.all_cells.cells:
            cell.properties['is_infected'] = infected_cell_locations[cell.coordinate]
            cell.properties['steps_since_infected'] = 0

        property_layer = PropertyLayer('is_infected', (grid_width, grid_height), False, bool)
        property_layer.data = infected_cell_locations

        self.grid.add_property_layer(property_layer)


    def get_random_infected_cell_distribution(self, grid_width: int, grid_height: int, infected_cells_count: int):
        cells_distribution = np.array([True] * infected_cells_count + [False] * (grid_width * grid_height - infected_cells_count))
        np.random.shuffle(cells_distribution)

        return cells_distribution.reshape((grid_width, grid_height))
    

    def init_data_collector(self):
        self.datacollector = DataCollector(
            model_reporters = {
                "Is_Infected": self.count_infected_agents,
                "Direct_Infection": self.count_direct_infections,
                "Location_Infection": self.count_indirect_infections,
            },
        )
        self.datacollector.collect(self)


    def count_infected_agents(self):
        return len(self._agents_by_type[PersonAgent].select(lambda agent: agent.is_infected))


    def count_direct_infections(self):
        return self.direct_interactions_count
    

    def count_indirect_infections(self):
        return self.location_interactions_count


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
        self.datacollector.collect(self)
        self._agents_by_type[PersonAgent].do("move_around")
        self._agents_by_type[PersonAgent].do("infect_others")
        self._agents_by_type[PersonAgent].do("random_infection_from_location")

        for cell in self.grid.all_cells.cells:
            if cell.properties.get('is_infected', False):
                if any(agent.is_infected for agent in cell.agents):
                    cell.properties['steps_since_infected'] = 0
                else:
                    cell.properties['steps_since_infected'] += 1
                if cell.properties['steps_since_infected'] > 2:
                    cell.properties['is_infected'] = False
                    cell.properties['steps_since_infected'] = 0
        
        infected_cell_data = np.array([cell.properties['is_infected'] for cell in self.grid.all_cells.cells]).reshape(self.grid.width, self.grid.height)

        self.grid.remove_property_layer('is_infected')
        property_layer = PropertyLayer('is_infected', (self.grid.width, self.grid.height), False, bool)
        property_layer.data = infected_cell_data
        self.grid.add_property_layer(property_layer)