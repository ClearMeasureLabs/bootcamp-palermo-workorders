param(
    [parameter(Mandatory = $true)][string]$aiResourceId,
    [parameter(Mandatory = $true)][string]$releaseName,
    [parameter(Mandatory = $false)]$releaseProperties = @()
)

# Function to ensure all Unicode characters in a JSON string are properly escaped
function Convert-UnicodeToEscapeHex {
  param (
    [parameter(Mandatory = $true)][string]$JsonString
  )
  $JsonObject = ConvertFrom-Json -InputObject $JsonString
  foreach ($property in $JsonObject.PSObject.Properties) {
    $name = $property.Name
    $value = $property.Value
    if ($value -is [string]) {
      $value = [regex]::Unescape($value)
      $OutputString = ""
      foreach ($char in $value.ToCharArray()) {
        $dec = [int]$char
        if ($dec -gt 127) {
          $hex = [convert]::ToString($dec, 16)
          $hex = $hex.PadLeft(4, '0')
          $OutputString += "\u$hex"
        }
        else {
          $OutputString += $char
        }
      }
      $JsonObject.$name = $OutputString
    }
  }
  return ConvertTo-Json -InputObject $JsonObject -Compress
}

$annotation = @{
    Id = [GUID]::NewGuid();
    AnnotationName = $releaseName;
    EventTime = (Get-Date).ToUniversalTime().GetDateTimeFormats("s")[0];
    Category = "Deployment"; #Application Insights only displays annotations from the "Deployment" Category
    Properties = ConvertTo-Json $releaseProperties -Compress
}

$annotation = ConvertTo-Json $annotation -Compress
$annotation = Convert-UnicodeToEscapeHex -JsonString $annotation

$accessToken = (az account get-access-token | ConvertFrom-Json).accessToken
$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Accept"        = "application/json"
    "Content-Type"  = "application/json"
}
$params = @{
    Headers = $headers
    Method  = "Put"
    Uri     = "https://management.azure.com$($aiResourceId)/Annotations?api-version=2015-05-01"
    Body    = $annotation
}
Invoke-RestMethod @params
