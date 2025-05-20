from camel.messages import BaseMessage
from camel.agents import ChatAgent
from camel.toolkits import FunctionTool, SearchToolkit
from humanlayer import HumanLayer
from linkup import LinkupClient
from config.config import HUMAN_LAYER_API_KEY, LINKUP_API_KEY 

h1 = HumanLayer(api_key=HUMAN_LAYER_API_KEY, verbose=True)
client = LinkupClient(api_key=LINKUP_API_KEY)

class PreferenceAgent(ChatAgent):
    agent_role = BaseMessage.make_assistant_message(
        role_name="preference_agent",
        content="""You are a preference agent. You are a helpful assistant that helps the user to choose between lucrative job offers.
        You should gather information about the user to search for job offers. Take into account the following factors:
        - Salary
        - Position Level
        - Technical Stack
        - Preferred Location

        Ask questions to understand the user's preferences if anything is unclear.
        """
    )

    ask_tool_schema = {
        "type": "function",
        "function": {
                "name": "ask_human",
                "description": "Ask a human a question and wait for the response. Response should be a string.",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "question": {
                            "type": "string",
                            "description": "The question to ask the human."
                        }
                    },
                    "required": ["question"],
                    "additionalProperties": False
                },
                "strict": True
            }
        }

    def ask_human(question: str):
        return h1.human_as_tool()(question)
    
    ask_human_tool = FunctionTool(ask_human, openai_tool_schema=ask_tool_schema)

    tools = [ask_human_tool]

    def __init__(self, model):
        super().__init__(model=model, system_message=self.agent_role, tools=self.tools)


class JobSearchAgent(ChatAgent):
    agent_role = BaseMessage.make_assistant_message(
        role_name="job_search_agent",
        content="""You are a job search agent. You are a helpful assistant that helps the user to find job offers.
        You connect with the linkup API and use it to search for positions based on user preferences. Preferences are gathered by the preference_agent. Your task is:
        - Taking the user preferences from the preference agent
        - Searching for job offers using the linkup API
        - Providing the user with a list of job offers that match their preferences
        - If the user provides a job offer, ask clarifying questions to understand the user's preferences and provide a recommendation based on the user's answers.
        """
    )

    search_jobs_schema = {
        "type": "function",
        "function": {
                "name": "search_jobs",
                "description": "Search for job offers using the linkup API using preferences collected by the PreferenceAgent.",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "query": {
                            "type": "string",
                            "description": "The query to search for job offers."
                        }
                    },
                    "required": ["query"],
                    "additionalProperties": False
                },
                "strict": True
            }
    }

    def search_jobs(query: str):
        return client.search(query=query, depth="standard", output_type="sourcedAnswer", include_images=False)
    
    search_jobs_tool = FunctionTool(search_jobs, openai_tool_schema=search_jobs_schema)

    tools = [search_jobs_tool]

    def __init__(self, model):
        super().__init__(model=model, system_message=self.agent_role, tools=self.tools)


class WebAgent(ChatAgent):
    agent_role = BaseMessage.make_assistant_message(
        role_name="web_agent",
        content="""You are a web agent. You are a helpful assistant that helps the user to find information on the web that will be helpful during the interviews.
        You can search for information on the web and provide the user with relevant links and summaries.
        You can tailor the tips to the user's preferences and the job offers they are considering.
        You can also provide the user with information about the company, the position, and the technical stack.
        At last, you should prepare a 2-week interview preparation plan for both theoretical and practical interviews, which should consist of daily goals, timeline, and resources to learn/repeat from.
        """
    )

    search_toolkit = SearchToolkit(timeout=5000)
    tools = [FunctionTool(search_toolkit.search_duckduckgo)]

    def __init__(self, model):
        super().__init__(model=model, system_message=self.agent_role, tools=self.tools)

