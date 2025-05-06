import solara

from mesa.visualization import SolaraViz, make_space_component, make_plot_component

from agents import PersonAgent
from model import InfectiousDiseaseSpreadModel


def agent_portrayal(agent):
    if isinstance(agent, PersonAgent):
        return {
            "color" : "tab:red" if agent.is_infected else "tab:blue",
            "size": 50 if agent.is_infected else 20,
            "shape": "square" if agent.has_comorbidities else "circle",
        }
    return {}


propertylayer_portrayal = {
    'is_infected': {
        'color': 'tab:red',
        'vmax': True,
        'vmin': False,
        'alpha': 0.3,
        'colorbar': False
    },
}


model_params = {
    "population_size": {
        "type": "SliderInt",
        "value": 15,
        "label": "Number of agents:",
        "min": 5,
        "max": 100,
        "step": 1,
    },
    "infected_population_size": {
        "type": "SliderInt",
        "value": 1,
        "label": "Number of infected agents:",
        "min": 1,
        "max": 10,
        "step": 1,
    },
    "comorbidities_population_size": {
        "type": "SliderInt",
        "value": 4,
        "label": "Number of agents with comorbidities:",
        "min": 0,
        "max": 10,
        "step": 1,
    },
    "infected_cells_count": {
        "type": "SliderInt",
        "value": 1,
        "label": "Number of infected cells:",
        "min": 1,
        "max": 10,
        "step": 1,
    },
    "grid_width": 10,
    "grid_height": 10,
}


model = InfectiousDiseaseSpreadModel()
grid_visualization = make_space_component(agent_portrayal, propertylayer_portrayal = propertylayer_portrayal)


page = SolaraViz(
    model,
    components = [grid_visualization],
    model_params = model_params,
    name = "Disease Spread Simulation",
)
page