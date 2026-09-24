param([string]$BaseUrl = 'http://127.0.0.1:5178')

$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')

function Assert-Equal($Actual, $Expected, [string]$Message) {
    if ($Actual -ne $Expected) { throw "$Message. Expected $Expected, got $Actual" }
}

function Send-Json([string]$Method, [string]$Path, $Body) {
    $parameters = @{ Uri = "$base$Path"; Method = $Method; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) {
        $parameters.Body = ConvertTo-Json $Body -Depth 8 -Compress
        $parameters.ContentType = 'application/json; charset=utf-8'
    }
    return Invoke-WebRequest @parameters
}

$page = Send-Json GET '/api/events?skip=0&limit=1' $null
Assert-Equal $page.StatusCode 200 'First page failed'
$pageData = $page.Content | ConvertFrom-Json
Assert-Equal $pageData.items.Count 1 'First page size'
Assert-Equal $pageData.skip 0 'First page skip'
if (-not $pageData.nextLink.StartsWith("$base/api/events?")) { throw 'Missing absolute nextLink' }
$nextPage = Invoke-RestMethod $pageData.nextLink
Assert-Equal $nextPage.skip 1 'Next page skip'
Assert-Equal (Send-Json GET '/api/events?skip=-1&limit=1' $null).StatusCode 400 'Negative skip validation'
Assert-Equal (Send-Json GET '/api/events?limit=101' $null).StatusCode 400 'Limit validation'
Assert-Equal (Send-Json GET '/api/events/999999' $null).StatusCode 404 'Missing event'

$stamp = [guid]::NewGuid().ToString('N').Substring(0, 8)
$organizerId = 0
$venueId = 0
$eventId = 0
try {
    $organizer = @{ name = "API Organizer $stamp"; description = 'Temporary test record'; contact = 'api@example.org'; websiteUrl = 'https://example.org' }
    $created = Send-Json POST '/api/organizers' $organizer
    Assert-Equal $created.StatusCode 201 'Organizer creation'
    $organizerId = ($created.Content | ConvertFrom-Json).id
    Assert-Equal (Send-Json GET "/api/organizers/$organizerId" $null).StatusCode 200 'Organizer read'

    $venue = @{ name = "API Venue $stamp"; address = 'Test address'; latitude = 50.45; longitude = 30.52; capacity = 25 }
    $created = Send-Json POST '/api/venues' $venue
    Assert-Equal $created.StatusCode 201 'Venue creation'
    $venueId = ($created.Content | ConvertFrom-Json).id
    Assert-Equal (Send-Json GET "/api/venues/$venueId" $null).StatusCode 200 'Venue read'

    $warm = Send-Json GET '/api/events?skip=0&limit=1' $null
    Assert-Equal $warm.Headers['X-Cache'][0] 'MISS' 'Cache warm-up after venue creation'
    $cached = Send-Json GET '/api/events?skip=0&limit=1' $null
    Assert-Equal $cached.Headers['X-Cache'][0] 'HIT' 'Cached page'

    $eventBody = @{ title = "API Event $stamp"; description = 'Temporary test event'; category = 'Test'; startsAt = '2030-01-01T15:00:00Z'; imageUrl = '/images/posters.svg'; organizerId = $organizerId; venueId = $venueId }
    $created = Send-Json POST '/api/events' $eventBody
    Assert-Equal $created.StatusCode 201 'Event creation'
    $eventId = ($created.Content | ConvertFrom-Json).id
    $afterCreate = Send-Json GET '/api/events?skip=0&limit=1' $null
    Assert-Equal $afterCreate.Headers['X-Cache'][0] 'MISS' 'Cache invalidation after event creation'
    $read = Send-Json GET "/api/events/$eventId" $null
    Assert-Equal $read.StatusCode 200 'Event read'
    if ($read.Content -notmatch '"startsAtUtc":"[^"]+Z"') { throw 'Event API time is not marked UTC' }

    Assert-Equal (Send-Json DELETE "/api/organizers/$organizerId" $null).StatusCode 409 'Linked organizer guard'
    Assert-Equal (Send-Json DELETE "/api/venues/$venueId" $null).StatusCode 409 'Linked venue guard'
    $eventBody.title = "Updated API Event $stamp"
    $updated = Send-Json PUT "/api/events/$eventId" $eventBody
    Assert-Equal $updated.StatusCode 200 'Event update'
    Assert-Equal ($updated.Content | ConvertFrom-Json).title $eventBody.title 'Updated event title'
    $eventBody.venueId = 999999
    Assert-Equal (Send-Json PUT "/api/events/$eventId" $eventBody).StatusCode 400 'Missing relation validation'
    Write-Output 'API smoke test passed: CRUD, status codes, relations, UTC, pagination.'
}
finally {
    if ($eventId -gt 0) { Assert-Equal (Send-Json DELETE "/api/events/$eventId" $null).StatusCode 204 'Event cleanup' }
    if ($venueId -gt 0) { Assert-Equal (Send-Json DELETE "/api/venues/$venueId" $null).StatusCode 204 'Venue cleanup' }
    if ($organizerId -gt 0) { Assert-Equal (Send-Json DELETE "/api/organizers/$organizerId" $null).StatusCode 204 'Organizer cleanup' }
}
