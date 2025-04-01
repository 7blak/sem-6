import pandas as pd
import matplotlib.pyplot as plt

def plot_direct_results(pop_size: int, avg_results: pd.DataFrame, ax: plt.Axes):
    subset = avg_results[avg_results["population_size"] == pop_size]
    ax.plot(subset["Step"], 
             subset["Direct_Interaction_Success_Ratio"], 
             label=f"Pop {pop_size}")
    
    ax.set_title("Direct Success Ratio Over Time")
    ax.set_xlabel("Step")
    ax.set_ylabel("Success ratio of sharing trend directly (%)")
    ax.legend()
    ax.grid(True, linestyle="--", alpha=0.6)


def plot_indirect_results(pop_size: int, avg_results: pd.DataFrame, ax: plt.Axes):
    subset = avg_results[avg_results["population_size"] == pop_size]
    ax.plot(subset["Step"], 
             subset["Indirect_Interaction_Success_Ratio"], 
             label=f"Pop {pop_size}", 
             linestyle="--")
    
    ax.set_title("Indirect Success Ratio Over Time")
    ax.set_xlabel("Step")
    ax.set_ylabel("Success ratio of sharing trend indirectly (%)")
    ax.legend()
    ax.grid(True, linestyle="--", alpha=0.6)


def plot_success_ratio_per_population_size_for_facility_no(results, facility_no: int = 9):
    results_df = pd.DataFrame(results)

    results_for_facility_no = results_df[results_df["sport_facility_number"] == facility_no]
    avg_results = results_for_facility_no.groupby(["Step", "population_size"]).mean().reset_index()
    population_sizes = results_df["population_size"].unique()

    _, ax = plt.subplots(1, 2, figsize=(14, 6), sharex=True)

    for pop_size in population_sizes:
        plot_direct_results(pop_size, avg_results, ax[0])
        plot_indirect_results(pop_size, avg_results, ax[1])

    plt.tight_layout()
    plt.show()