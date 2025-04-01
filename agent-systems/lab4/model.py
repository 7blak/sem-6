import logging
import numpy as np

from mesa import Model
from mesa.experimental.cell_space import OrthogonalVonNeumannGrid, PropertyLayer
from mesa.datacollection import DataCollector

from agents import PersonAgent, InfluencerAgent

logger = logging.getLogger(__name__)


class TrendSpreadModel(Model):
    '''
    Model class that simulates the spread of a trend in a population.

    Attributes:
    population_size (int): Total number of agents in the model.
    influencer_group_size (int): Number of influencer agents in the model.
    direct_interactions_count (int): Count of direct agent interactions.
    indirect_interactions_count (int): Count of indirect agent interactions.
    successful_direct_interactions_count (int): Count of successful trend spread due to direct interactions.
    successful_indirect_interactions_count (int): Count of successful trend spread due to indirect interactions.
    seed (int): Seed for random number generation.
    '''
    population_size: int
    influencer_group_size: int
    direct_interactions_count: int
    indirect_interactions_count: int
    successful_direct_interactions_count: int
    successful_indirect_interactions_count: int
    datacollector: DataCollector
    grid: OrthogonalVonNeumannGrid
    seed: int


    def __init__(self, 
                 population_size: int = 5, 
                 influencer_group_size: int = 1,
                 sport_facility_number: int = 3,
                 grid_width: int = 10, 
                 grid_height: int = 10, 
                 seed=None):
        super().__init__(seed=seed)
        self.population_size = population_size
        self.influencer_group_size = influencer_group_size
        
        self.direct_interactions_count = 0
        self.indirect_interactions_count = 0
        self.successful_direct_interactions_count = 0
        self.successful_indirect_interactions_count = 0

        self.grid = OrthogonalVonNeumannGrid((grid_width, grid_height), True, random=self.random)

        self.generate_person_agents()
        self.generate_influencer_agents()
        # self.generate_sport_facility_locations(grid_width, grid_height, sport_facility_number)
        # self.initialize_data_collector()


    def generate_person_agents(self):
        '''
        Method generates regular person agents and places them on the grid.
        '''
        sport_enthusiasts = self.random.randint(1, self.population_size)
        non_sport_enthusiasts = self.population_size - sport_enthusiasts

        PersonAgent.create_agents(self, sport_enthusiasts, sport_enthusiast=True, 
                                  knows_about_trend=False, skepticism_prob=0.5)
        PersonAgent.create_agents(self, non_sport_enthusiasts, sport_enthusiast=False, 
                                  knows_about_trend=False)
        PersonAgent(self, sport_enthusiast=True, knows_about_trend=True)
        self.population_size += 1

        self.place_agents_on_grid()

    
    def generate_influencer_agents(self):
        '''
        Method generates influencer agents.
        Important! These agents do not operate on the grid!
        '''
        InfluencerAgent.create_agents(self, self.influencer_group_size)


    def place_agents_on_grid(self):
        '''
        Method that randomly places agents on the grid.
        '''
        x_range = self.rng.integers(0, self.grid.width, size=(self.population_size,))
        y_range = self.rng.integers(0, self.grid.height, size=(self.population_size,))

        for agent, x_coord, y_coord in zip(self._agents_by_type[PersonAgent], x_range, y_range):
            agent.cell = self.grid[(x_coord, y_coord)]


    def generate_sport_facility_locations(self, grid_width: int, 
                                          grid_height: int, 
                                          sport_facility_number: int):
        '''
        Method initializes the locations of sport facilities both on the grid and on cells.
        '''
        sport_facility_locations = self.get_random_facility_distribution(
            grid_width, grid_height, sport_facility_number)

        for cell in self.grid.all_cells.cells:
            cell.properties['sport_facility'] = sport_facility_locations[cell.coordinate]
        
        property_layer = PropertyLayer('sport_facility', (grid_width, grid_height), False, bool)
        property_layer.data = sport_facility_locations

        self.grid.add_property_layer(property_layer)


    def get_random_facility_distribution(self, grid_width: int, 
                                         grid_height: int, 
                                         sport_facility_number: int):
        '''
        Method that returns the distribution of sport facilities on the grid.
        '''
        cells_distribution = np.array([True] * sport_facility_number + 
                                      [False] * (grid_width * grid_height - sport_facility_number))
        np.random.shuffle(cells_distribution)

        return cells_distribution.reshape((grid_width, grid_height))
    

    def initialize_data_collector(self):
        '''
        Method initializes the data collector for the model.
        ''' 
        self.datacollector = DataCollector(
            model_reporters={
                "Knows_Trend": self.count_agents_knowing_trend,
                "Direct_Interaction_Success_Ratio": self.direct_interaction_success_ration,
                "Indirect_Interaction_Success_Ratio": self.indirect_interaction_success_ration,
            },
            agenttype_reporters={
                PersonAgent: {
                    "Trend_Encounters": "trend_gossip_counter",
                },
                InfluencerAgent: {
                    "Followers_Influenced": "reached_followers_counter",
                },
            }
        )
        self.datacollector.collect(self)


    def count_agents_knowing_trend(self):
        return len(self._agents_by_type[PersonAgent].select(lambda agent: agent.knows_about_trend))
    

    def count_all_interactions(self):  
        return self.direct_interactions_count + self.indirect_interactions_count
    

    def direct_interaction_success_ration(self):
        return 0 if self.direct_interactions_count == 0 else\
              self.successful_direct_interactions_count / self.direct_interactions_count
    

    def indirect_interaction_success_ration(self):
        return 0 if self.indirect_interactions_count == 0 else\
            self.successful_indirect_interactions_count / self.indirect_interactions_count


    def _stop_condition(step) -> None:
        '''
        Method that checks if the simulation should stop.
        '''
        def perform_step(self):
            if len(self._agents_by_type[PersonAgent].select(lambda agent: not agent.knows_about_trend)) == 0:
                self.running = False
                logger.info("All agents know about the trend. Stopping the simulation.")
            else:
                step(self)

        return perform_step


    @_stop_condition
    def step(self):
        '''
        Method that advances the simulation by one tick.
        ''' 
        # self.datacollector.collect(self)
        self._agents_by_type[PersonAgent].do('move_around')
        self._agents_by_type[PersonAgent].do('spread_news')

        self.influencer_agents_step()


    def influencer_agents_step(self):
        '''
        Method that advances the simulation by one tick for influencer agents.
        '''
        regular_agents = self.agents.select(lambda agent: isinstance(agent, PersonAgent) 
                                            and not agent.knows_about_trend)
        influencer_agents = self.agents.select(lambda agent: isinstance(agent, InfluencerAgent))
        grouped_regular_agents = regular_agents.groupby('sport_enthusiast')

        for sport_enthusiast, agents in grouped_regular_agents:
            if sport_enthusiast:
                influencer_agents.do('spread_news_among_random_followers', agents)
            else:
                agents.do('move_around')

        # influencer_agents.do('spread_news_on_event', regular_agents)
        



    
        
        


    