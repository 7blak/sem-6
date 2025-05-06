import logging

from typing import Literal

from mesa import Model, Agent
from mesa.agent import AgentSet
from mesa.experimental.cell_space import Grid2DMovingAgent

logging.basicConfig(level=logging.INFO, format = '%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class PersonAgent(Grid2DMovingAgent):
    _type: Literal['PersonAgent'] = 'PersonAgent'
    _is_infected: bool
    _has_comorbidities: bool
    _moving_probability: float


    def __init__(self, model: Model, 
                 is_infected: bool = False,
                 has_comorbidities: bool = False,
                 moving_probability: float = 0.5):
        super().__init__(model)

        self.model = model
        self._is_infected = is_infected
        self._has_comorbidities = has_comorbidities
        self._moving_probability = moving_probability
    

    @property
    def is_infected(self) -> bool:
        return self._is_infected
    

    def become_infected(self, agent_id: str, is_direct: bool):
        self._is_infected = True
        logger.info(f'[{self._type} {self.unique_id}] I am now infected.')
    

    @property
    def has_comorbidities(self) -> bool:
        return self._has_comorbidities
    

    def move_around(self):
        self.check_grid_initialization(error_msg = "The agent cannot move around the grid.")
        if self.random.random() < self._moving_probability:
            prev_pos = self.cell.coordinate
            self.move_to(self.cell.neighborhood.select_random_cell())
            # logger.info(f'[{self._type} {self.unique_id}] Moved from {prev_pos} to {self.cell.coordinate}.')


    def infect_others(self):
        self.check_grid_initialization(error_msg = "The agent cannot infect others.")

        if self.is_infected:
            if not self.cell.properties.get('is_infected', False):
                self.cell.properties['is_infected'] = True
            self.cell.properties['steps_since_infected'] = 0
            encountered_agents = [agent for agent in self.cell.agents if agent is not self]

            for agent in encountered_agents:
                infection_probability = 0.75 if agent.has_comorbidities else 0.5
                if not agent.is_infected and self.random.random() < infection_probability:
                    agent.become_infected(self.unique_id, is_direct = True)


    def random_infection_from_location(self):
        self.check_grid_initialization(error_msg = "The agent cannot get infected (does not exist on grid).")

        if not self.is_infected:
            if self.cell.properties.get('is_infected', False):
                if self.has_comorbidities:
                    infection_probability = 0.75
                else:
                    infection_probability = 0.5
            elif self.check_neighbourhood_for_infection():
                if self.has_comorbidities:
                    infection_probability = 0.5
                else:
                    infection_probability = 0.25
            else:
                infection_probability = 0.0
            if self.random.random() < infection_probability:
                logger.info(f'[{self._type} {self.unique_id}] I got infected from my location.')
                self.become_infected(self.unique_id, is_direct = False)


    def check_neighbourhood_for_infection(self):
        self.check_grid_initialization(error_msg="Cannot check neighbourhood for infection.")

        neighbourhood = self.cell.neighborhood
        for cell in neighbourhood:
            if cell.properties.get('is_infected', False):
                return True
        return False


    def check_grid_initialization(self, error_msg: str = ''):
        if self.cell is None:
            logger.error("Couldn't perform operation on the grid: {error_msg}.")
            raise ValueError(f'The grid has not been initialized: {error_msg}')
