import logging

from config.config import API_KEY

from camel.configs import ChatGPTConfig
from camel.models import ModelFactory, BaseModelBackend
from camel.types import ModelPlatformType

class DefaultModel:
    '''
    Class contains model singletons and static methods for dynamic model creation.
    '''
    ollama_model: BaseModelBackend = None # type: ignore
    openai_model: BaseModelBackend = None # type: ignore

    @classmethod
    def create_openai_model(cls) -> BaseModelBackend:
        '''
        Create an OpenAI model instance if it doesn't exist, otherwise return the existing instance.
        '''
        if not cls.openai_model:
            cls.openai_model = ModelFactory.create(
                model_platform=ModelPlatformType.OPENAI,
                model_type="gpt-4o",
                model_config_dict={"temperature": 0.5},
                api_key=API_KEY
            )
        else:
            logging.warning("Model already initialized, returning existing instance")
        
        return cls.openai_model


    @classmethod
    def create_local_model(cls) -> BaseModelBackend:
        '''
        Create a local model instance if it doesn't exist, otherwise return the existing instance.
        '''
        if not cls.ollama_model:
            cls.ollama_model = ModelFactory.create(
                model_platform=ModelPlatformType.OLLAMA,
                model_type="llama3",
                model_config_dict={"temperature": 0.5}
            )
        else:
            logging.warning("Model already initialized, returning existing instance")
        
        return cls.ollama_model
    

    @staticmethod
    def create_custom_openai_model(model_type: str, n: int = 1) -> BaseModelBackend:
        '''
        Create a custom OpenAI model instance.
        '''
        return ModelFactory.create(
            model_platform=ModelPlatformType.OPENAI,
            model_type=model_type,
            model_config_dict=ChatGPTConfig(temperature=0.5, n=n).as_dict(),
            api_key=API_KEY
        )


    