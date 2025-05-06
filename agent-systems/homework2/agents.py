import logging

from typing import Literal

from mesa import Model, Agent
from mesa.agent import AgentSet
from mesa.experimental.cell_space import Grid2DMovingAgent

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
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
        logger.info(f'[{self._type} {self.unique_id}] My current position is {self.pos}.')
        self.move_to(self.cell.neighborhood.select_random_cell())

    def infect_others(self):
        self.check_grid_initialization(error_msg = "The agent cannot infect others.")

        if self.is_infected:
            encountered_agents = [agent for agent in self.cell.agents if agent is not self]

            for agent in encountered_agents:
                if not agent.is_infected and self.random.random() < 0.2:
                    agent.become_infected(self.unique_id, is_direct = True)


    def check_grid_initialization(self, error_msg: str = ''):
        if self.cell is None:
            logger.error("Couldn't perform operation on the grid: {error_msg}.")
            raise ValueError(f'The grid has not been initialized: {error_msg}')
