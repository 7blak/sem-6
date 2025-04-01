import logging

from typing import Literal

from mesa import Model, Agent
from mesa.agent import AgentSet
from mesa.experimental.cell_space import Grid2DMovingAgent

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)


class TrendSpreadingAgent():
    '''
    Base class for agents in the trend spreading model.

    Attributes:
    model (Model): Reference to the Mesa model instance.
    grid (MultiGrid): Reference to the Mesa grid instance.
    type (str): Type of the agent, either 'RegularPerson' or 'SportInfluencer'.
    '''
    _type: Literal['RegularPerson', 'SportInfluencer']

    def __init__(self):
        self._type = 'RegularPerson' if isinstance(self, PersonAgent) else 'SportInfluencer'


    def introduce_self(self):
        '''
        Method introducing the agent.
        '''
        print(f'Hello, I am {self._type}. My unique id is {self.unique_id}.')


    def check_grid_initialization(self, error_msg: str = ''):
        '''
        Method that checks if the grid is initialized and raises an error with a custom message if not.

        Attributes:
        error_msg (str): Custom message to be displayed in the error.
        '''
        if self.cell is None:
            logger.error("Couldn't perform operation on the grid: {error_msg}.")
            raise ValueError(f'The grid has not been initialized: {error_msg}')


class PersonAgent(Grid2DMovingAgent, TrendSpreadingAgent):
    '''
    Agent represents a regular person that performs 2 actions:
    1. Move around the grid.
    2. Spread the news about a trend among other agents.

    Attributes:
    sport_enthusiast (bool): Indicates if the agent is a sport enthusiast.
    trend_gossip_counter (int): Counter for the number of times the agent has heard the trend.
    _knows_about_trend (bool): Indicates if the agent knows about the trend.
    _gossip_prob (float): Probability of the agent spreading the information.
    _skepticism_prob (float): Probability of the agent to follow the trend.
    '''
    sport_enthusiast: bool
    trend_gossip_counter: int
    _knows_about_trend: bool
    _gossip_prob: float
    _skepticism_prob: float


    def __init__(self, model: Model, 
                 sport_enthusiast: bool=True,
                 knows_about_trend: bool=False,
                 gossip_prob: float=0.5,
                 skepticism_prob: float=0.7):
        super().__init__(model)
        
        self.sport_enthusiast = sport_enthusiast
        self.trend_gossip_counter = 0
        self._gossip_prob = gossip_prob
        self._knows_about_trend = knows_about_trend
        self._skepticism_prob = skepticism_prob


    @property
    def knows_about_trend(self) -> bool:
        '''
        Property indicating if the agent knows about the trend.
        '''
        return self._knows_about_trend
    

    def learn_about_trend(self, agent_id: str, is_direct: bool):
        '''
        Method that makes the agent learn about the trend.

        Attributes:
        agent_id (str): Unique ID of the agent that shared the trend.
        '''
        self.trend_gossip_counter += 1

        skepticism_prob = 0.2 
        # if self.cell.properties['sport_facility'] else self._skepticism_prob
        
        if self.random.random() > skepticism_prob:
            self._knows_about_trend = True
            logger.info(f'[{self._type} {self.unique_id}] I learned about the trend from {agent_id}!')
            
            if is_direct:
                self.model.successful_direct_interactions_count += 1
            else:
                self.model.successful_indirect_interactions_count += 1

        else:
            logger.warning(f'[{self._type} {self.unique_id}] I am not going to follow the trend shared by {agent_id}.')


    def move_around(self):
        '''
        Method that moves the agent around the grid.
        Important! The grid must be initialized as part of the model under the variable with name 'grid'.
        '''
        self.check_grid_initialization(error_msg='The agent cannot move around the grid.')
        
        logger.info(f'[{self._type} {self.unique_id}] My current position is {self.pos}.')

        self.move_to(self.cell.neighborhood.select_random_cell())

        # Another alternative that can be used:
        # self.move(self.random.choice(['north', 'south', 'east', 'west']))


    def spread_news(self):
        '''
        Method that makes the agent spread the news among other agents encountered at the same grid cell.
        '''
        self.check_grid_initialization(error_msg='The agent cannot spread the news.')
        
        if self.knows_about_trend:
            encountered_agents = [agent for agent in self.cell.agents if agent is not self]

            for agent in encountered_agents:
                if not agent.knows_about_trend and \
                    (agent.sport_enthusiast or self.random.random() < self._gossip_prob):
                    agent.model.direct_interactions_count += 1
                    agent.learn_about_trend(self.unique_id, True)
        else:
            logger.warning(f"[{self._type} {self.unique_id}] I don't know anything that I could share.")
        

class InfluencerAgent(Agent, TrendSpreadingAgent):
    '''
    Agent represents a sport influencer that can spread the trend among their followers.

    Attributes:
    reached_followers_counter (int): Counter for the number of followers reached by the influencer.
    '''
    reached_followers_counter: int

    def __init__(self, model):
        super().__init__(model)

        self.reached_followers_counter = 0


    def spread_news_among_random_followers(self, followers: AgentSet):
        '''
        Method that makes the agent spread the news among their followers.
        '''
        for agent in followers:
            if self.random.random() < 0.5:
                logger.info(f'[{self._type} {self.unique_id}] I passed information about the trend to {agent.unique_id}!')

                self.reached_followers_counter += 1
                self.model.indirect_interactions_count += 1
                agent.learn_about_trend(self.unique_id, False)

    
    def spread_news_on_event(self, event_participants: AgentSet):
        '''
        Method that makes the agent spread the event participants.
        '''
        for agent in event_participants:
            if agent.cell.properties['sport_facility']:
                logger.info(f'[{self._type} {self.unique_id}] I passed information about the trend to {agent.unique_id}!')

                self.reached_followers_counter += 1
                self.model.indirect_interactions_count += 1
                agent.learn_about_trend(self.unique_id, False)


    