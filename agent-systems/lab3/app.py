from model import TrendModel
from mesa.visualization import SolaraViz, make_space_component

def agent_portrayal(agent):
    return {
        "color": "tab:red" if agent.knows_about_trend else "tab:blue",
        "marker": "x" if agent.sport_enthusiast else "o",
        "size": 50
    }

model_params = {
    "population_size": {
        "type": "SliderInt",
        "value": 50,
        "label": "Number of agents",
        "min": 2,
        "max": 20,
        "step": 1
    },
    "width": 10,
    "height": 10
}

model = TrendModel(10, seed=2)
grid_visualziation = make_space_component(agent_portrayal)

page = SolaraViz(
    model,
    components=[grid_visualziation],
    model_params=model_params,
    name="Trend Model"
)
page