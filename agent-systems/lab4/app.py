import solara

from matplotlib.figure import Figure
from mesa.visualization import SolaraViz, make_space_component, make_plot_component

from agents import PersonAgent
from model import TrendSpreadModel

def agent_portrayal(agent):
    if isinstance(agent, PersonAgent):
        return {
            "color": "tab:red" if agent.knows_about_trend else "tab:blue",
            "size": 50 if agent.knows_about_trend else 30,
            "marker": "x" if agent.sport_enthusiast else "o",
        }
    return {}


propertylayer_portrayal = {
    'sport_facility': {
        'color': 'tab:green',
        'vmax': True,
        'vmin': False,
        'alpha': 0.3,
        'colorbar': False
    },
}

model_params = {
    "population_size": {
        "type": "SliderInt",
        "value": 20,
        "label": "Number of agents:",
        "min": 10,
        "max": 20,
        "step": 1,
    },
    "influencer_group_size": {
        "type": "SliderInt",
        "value": 1,
        "label": "Number of influencer agents:",
        "min": 1,
        "max": 5,
        "step": 1,
    },
    "sport_facility_number": {
        "type": "SliderInt",
        "value": 3,
        "label": "Number of sport facilities:",
        "min": 1,
        "max": 10,
        "step": 1,
    },
    "grid_width": 10,
    "grid_height": 10,
}


def get_total_interactions(model: TrendSpreadModel):
    total_interactions = model.count_all_interactions()
    return solara.Markdown(f"Total interactions between agents: {total_interactions}")


def get_trend_spread_per_agent(model: TrendSpreadModel):
    trend_spread_per_agent = model.datacollector.get_agenttype_vars_dataframe(PersonAgent)

    fig = Figure()
    ax = fig.subplots()

    latest_tick_data = trend_spread_per_agent.index.get_level_values("Step").max()
    latest_data = trend_spread_per_agent.xs(latest_tick_data, level="Step")

    agent_ids = latest_data.index
    trend_encounters = latest_data['Trend_Encounters']
    
    ax.bar(agent_ids, trend_encounters, color='tab:blue')

    ax.set_title(f"Trend Encounter Count per Agent")
    ax.set_xlabel("Agent ID")
    ax.set_ylabel("Trend Encounter Count")
    ax.set_xticks(agent_ids)
    ax.set_yticks(range(0, int(trend_encounters.max()) + 1, 1))
    ax.set_xticklabels(agent_ids, rotation=45, ha="right")

    return solara.FigureMatplotlib(fig)


model = TrendSpreadModel(10, 1, 3, 10, 10)
grid_visualization = make_space_component(agent_portrayal)
trend_spread_visualization = make_plot_component('Knows_Trend')
trend_spread_success = make_plot_component({"Direct_Interaction_Success_Ratio": "tab:red", 
                                            "Indirect_Interaction_Success_Ratio": "tab:green"})


page = SolaraViz(
    model,
    components=[grid_visualization], 
                # trend_spread_visualization,
                # trend_spread_success,
                # get_trend_spread_per_agent,
                # get_total_interactions],
    model_params=model_params,
    name="Trend Spreading Model",
)
page