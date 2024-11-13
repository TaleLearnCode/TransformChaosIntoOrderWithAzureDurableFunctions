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

variable "acs_data_location" {
  type        = string
  default     = "United States"
  description = "The location where the Communication service stores its data at rest."
}

# #############################################################################
# Data Sources
# #############################################################################

data "azurerm_client_config" "current" {}

# #############################################################################
# Resources
# #############################################################################

module "azure_region" {
  source       = "TaleLearnCode/regions/azurerm"
  version      = "0.0.1-pre"
  azure_region = var.location
}

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

resource "azurerm_communication_service" "communication_service" {
  name                = "acs-${var.srv_comp_abbr}-${var.environment}-${module.azure_region.region.region_short}"
  resource_group_name = module.rg.resource_group.name
  data_location       = var.acs_data_location
}

resource "azurerm_email_communication_service" "email_communication_service" {
  name                = "acsemail-${var.srv_comp_abbr}-${var.environment}-${module.azure_region.region.region_short}"
  resource_group_name = module.rg.resource_group.name
  data_location       = var.acs_data_location
}

resource "azurerm_email_communication_service_domain" "email_communication_service_domain" {
  name              = "acsemaildomain-${var.srv_comp_abbr}-${var.environment}-${module.azure_region.region.region_short}"
  email_service_id  = azurerm_email_communication_service.email_communication_service.id
  domain_management = "AzureManaged"
}

resource "azurerm_communication_service_email_domain_association" "communication_service_email_domain_association" {
  communication_service_id = azurerm_communication_service.communication_service.id
  email_service_domain_id  = azurerm_email_communication_service_domain.email_communication_service_domain.id
}