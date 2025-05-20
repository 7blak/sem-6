from model import DefaultModel
from agents import PreferenceAgent, JobSearchAgent, WebAgent
from camel.societies.workforce import Workforce
from camel.tasks import Task
from camel.messages import BaseMessage

model = DefaultModel.create_openai_model()

preference_agent = PreferenceAgent(model=model)
job_search_agent = JobSearchAgent(model=model)
web_agent = WebAgent(model=model)

workforce = Workforce("Job search and interview preparation")

workforce.add_single_agent_worker("Agent retrieving information about user preferences for job offers", worker=preference_agent)
workforce.add_single_agent_worker("Agent searching for job offers", worker=job_search_agent)
workforce.add_single_agent_worker("Agent searching for information on the web in order to help the user prepare for the interview", worker=web_agent)

task = Task(content="""
            1. Collect detailed user preferences for job offers
            2. Search for matching position and return results sorted by salary (descending) with:
            - Salary
            - Position Level
            - Technical Stack
            - Location
            3. Search for information on the web that will be helpful during the interviews
            4. Prepare a 2-week interview preparation plan for the user for both theoretical and practical interviews, which should include:
                - Daily goals
                - Timeline
                - Resources to learn/repeat from
            5. Provide the user with a summary of the information found
            """,
            id="0"
    )

task = workforce.process_task(task)

print('Final result of task:\n', task.result)