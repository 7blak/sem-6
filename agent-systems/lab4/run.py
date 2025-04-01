import logging
import matplotlib.pyplot as plt

from mesa.batchrunner import batch_run
from model import TrendSpreadModel
from helpers.plot_utils import plot_success_ratio_per_population_size_for_facility_no


logging.disable(logging.INFO)
logging.disable(logging.WARNING)


batch_run_result = batch_run(
    model_cls=TrendSpreadModel,
    parameters={
        "population_size": [10, 30, 50],
        "influencer_group_size": 1,
        "sport_facility_number": range(1, 10, 2),
        "grid_width": 10,
        "grid_height": 10,
        "seed": 42
    },
    iterations=5,
    max_steps=20,
    number_processes=1,
    data_collection_period=5,
    display_progress=True
)

plot_success_ratio_per_population_size_for_facility_no(batch_run_result, facility_no=9)