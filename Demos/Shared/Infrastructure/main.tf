# #############################################################################
# Provider Configuration
# #############################################################################

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>4.8"
    }
  }
}

provider "azurerm" {
  subscription_id = var.subscription_id
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
  }
}

# #############################################################################
# Variables
# #############################################################################

variable "subscription_id" {
  description = "The Azure subscription ID"
}

variable "environment" {
  type        = string
  description = "The environment to deploy the infrastructure to."
}

variable "location" {
  type        = string
  description = "The Azure region to deploy the infrastructure to."
}

variable "srv_comp_abbr" {
  type        = string
  default     = "adf"
  description = "The abbreviation of the service component."
}

# #############################################################################
# Data Sources
# #############################################################################

data "azurerm_client_config" "current" {}

# #############################################################################
# Resources
# #############################################################################

module "rg" {
  source  = "TaleLearnCode/resource_group/azurerm"
  version = "0.0.1-pre"
  providers = {
    azurerm = azurerm
  }

  srv_comp_abbr = var.srv_comp_abbr
  environment   = var.environment
  location      = var.location
}

module "appcs" {
  source  = "TaleLearnCode/app_configuration/azurerm"
  version = "0.0.3-pre"
  providers = {
    azurerm = azurerm
  }

  location                       = var.location
  environment                    = var.environment
  resource_group_name            = module.rg.resource_group.name
  srv_comp_abbr                  = var.srv_comp_abbr
  app_configuration_data_owners  = [ data.azurerm_client_config.current.object_id ]

  depends_on = [ module.rg ]
}