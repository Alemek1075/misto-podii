param([string]$BaseUrl = 'http://127.0.0.1:5178')

$ErrorActionPreference = 'Stop'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$base = $BaseUrl.TrimEnd('/')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Get-Page([string]$Path) {
    return Invoke-WebRequest -Uri "$base$Path" -WebSession $session
}

function Submit-Form([string]$Path, [hashtable]$Fields) {
    $page = Get-Page $Path
    $token = [regex]::Match($page.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
    Assert-True ($token.Length -gt 0) "Missing CSRF token at $Path"
    $Fields['__RequestVerificationToken'] = $token
    return Invoke-WebRequest -Uri "$base$Path" -WebSession $session -Method Post -Body $Fields -ContentType 'application/x-www-form-urlencoded'
}

function Get-DetailId($Response, [string]$Entity) {
    $path = $Response.BaseResponse.RequestMessage.RequestUri.AbsolutePath
    $match = [regex]::Match($path, "^/$Entity/Details/(\d+)$")
    Assert-True $match.Success "Expected $Entity details after save, got $path"
    return [int]$match.Groups[1].Value
}

foreach ($path in @('/', '/Events', '/Events/Details/1', '/Venues', '/Organizers')) {
    $response = Get-Page $path
    Assert-True ($response.StatusCode -eq 200) "GET $path failed"
}

$blocked = Submit-Form '/Organizers/Delete/1' @{}
Assert-True ([System.Net.WebUtility]::HtmlDecode($blocked.Content).Contains('Спершу видаліть або перенесіть події')) 'Linked organizer deletion was not blocked'

$stamp = [guid]::NewGuid().ToString('N').Substring(0, 8)
$organizerId = 0
$venueId = 0
$eventId = 0
try {
    $created = Submit-Form '/Organizers/Create' @{ Name = "Smoke Organizer $stamp"; Description = 'Temporary test record'; Contact = 'smoke@example.org'; WebsiteUrl = 'https://example.org' }
    $organizerId = Get-DetailId $created 'Organizers'
    $created = Submit-Form '/Venues/Create' @{ Name = "Smoke Venue $stamp"; Address = 'Test address'; Latitude = '50.45'; Longitude = '30.52'; Capacity = '25' }
    $venueId = Get-DetailId $created 'Venues'
    $created = Submit-Form '/Events/Create' @{ Title = "Smoke Event $stamp"; Description = 'Temporary test event'; Category = 'Test'; StartsAtUtc = '2030-01-01T15:00'; ImageUrl = '/images/posters.svg'; OrganizerId = "$organizerId"; VenueId = "$venueId" }
    $eventId = Get-DetailId $created 'Events'
    $updated = Submit-Form "/Events/Edit/$eventId" @{ Id = "$eventId"; Title = "Updated Smoke Event $stamp"; Description = 'Temporary test event'; Category = 'Test'; StartsAtUtc = '2030-01-01T15:00'; ImageUrl = '/images/posters.svg'; OrganizerId = "$organizerId"; VenueId = "$venueId" }
    Assert-True ($updated.Content.Contains("Updated Smoke Event $stamp")) 'Event edit was not saved'
    $catalog = Get-Page '/?q=Updated%20Smoke%20Event'
    Assert-True ($catalog.Content.Contains("Updated Smoke Event $stamp")) 'Event search did not find the edited record'
    Write-Output 'MVC smoke test passed: pages, linked-delete guard, create, edit, search.'
}
finally {
    if ($eventId -gt 0) { $null = Submit-Form "/Events/Delete/$eventId" @{} }
    if ($venueId -gt 0) { $null = Submit-Form "/Venues/Delete/$venueId" @{} }
    if ($organizerId -gt 0) { $null = Submit-Form "/Organizers/Delete/$organizerId" @{} }
}
