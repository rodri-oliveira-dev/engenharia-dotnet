variable "ambiente" {
  type        = string
  description = "Ambiente lógico do manifesto."
  default     = "desenvolvimento"

  validation {
    condition     = contains(["desenvolvimento", "homologacao"], var.ambiente)
    error_message = "Use desenvolvimento ou homologacao neste laboratório."
  }
}

data "local_file" "modelo" {
  filename = "${path.module}/modelo.txt"
}

resource "local_file" "manifesto" {
  filename        = "${path.module}/saida/manifesto.txt"
  content         = replace(data.local_file.modelo.content, "$${ambiente}", var.ambiente)
  file_permission = "0644"
}

output "manifesto" {
  description = "Caminho do arquivo gerenciado pelo laboratório."
  value       = local_file.manifesto.filename
}

