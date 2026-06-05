/**
 * AlinasRailTools Tile Generator - Web Interface
 */

const App = {
    map: null,
    baseLayers: {},
    tileProviders: [],
    gameTilesLayer: null,
    gridLayer: null,
    currentMap: null,
    selectedTiles: new Set(),
    generatedTiles: new Set(),
    tileRectangles: new Map(),
    drawnItems: null,
    drawControl: null,
    isGenerating: false,

    MIN_GRID_ZOOM: 11,
    MAX_GRID_TILES: 400,
    TILE_BUFFER: 2,

    async init() {
        // Initialize Leaflet map
        this.map = L.map('map-container', {
            center: [35.3826, -83.4954], // Default to BushnellWhittier origin
            zoom: 13,
            zoomControl: true
        });

        // Load available tile providers
        await this.loadTileProviders();

        // Create tile layer for game tiles (will be set when map is loaded)
        this.gameTilesLayer = null;

        // Create grid layer (initially empty)
        this.gridLayer = L.layerGroup();

        // Add layer control (will be updated when map is loaded)
        this.layerControl = null;

        // Add grid layer to map by default
        this.gridLayer.addTo(this.map);

        // Initialize Leaflet Draw
        this.initDrawControls();

        // Set up event listeners
        this.setupEventListeners();

        // Load initial data
        await this.loadMaps();
        await this.loadCacheStats();

        // Enable click to select tiles (only when a map is loaded)
        this.map.on('click', (e) => this.onMapClick(e));
    },

    initDrawControls() {
        // Create a layer group for drawn items
        this.drawnItems = new L.FeatureGroup();
        this.map.addLayer(this.drawnItems);

        // Configure draw control with only rectangle and polygon
        this.drawControl = new L.Control.Draw({
            position: 'topright',
            draw: {
                polyline: false,
                circle: false,
                circlemarker: false,
                marker: false,
                rectangle: {
                    shapeOptions: {
                        color: '#3388ff',
                        weight: 2,
                        fillOpacity: 0.2
                    }
                },
                polygon: {
                    shapeOptions: {
                        color: '#3388ff',
                        weight: 2,
                        fillOpacity: 0.2
                    }
                }
            },
            edit: {
                featureGroup: this.drawnItems,
                remove: true
            }
        });
        this.map.addControl(this.drawControl);

        // Handle drawn shapes
        this.map.on(L.Draw.Event.CREATED, (e) => this.onRegionDrawn(e));
    },

    async loadTileProviders() {
        try {
            const response = await fetch('/api/tile-providers');
            const data = await response.json();
            this.tileProviders = data.providers;

            // Create Leaflet layers for each provider
            let firstLayer = null;
            this.tileProviders.forEach((provider, index) => {
                const layer = L.tileLayer(`/api/tiles/${provider.id}/{z}/{x}/{y}`, {
                    maxZoom: 18,
                    attribution: provider.name
                });

                this.baseLayers[provider.name] = layer;

                // Add first layer to map by default
                if (index === 0) {
                    layer.addTo(this.map);
                    firstLayer = layer;
                }
            });

            console.log(`Loaded ${this.tileProviders.length} tile providers`);
        } catch (error) {
            console.error('Failed to load tile providers:', error);
        }
    },

    setupEventListeners() {
        document.getElementById('map-selector').addEventListener('change', (e) => {
            this.loadMap(e.target.value);
        });

        document.getElementById('new-map-btn').addEventListener('click', () => {
            this.showNewMapModal();
        });

        document.getElementById('new-map-form').addEventListener('submit', (e) => {
            e.preventDefault();
            this.createNewMap();
        });

        document.getElementById('cancel-new-map').addEventListener('click', () => {
            this.hideNewMapModal();
        });


        document.getElementById('generate-btn').addEventListener('click', () => {
            this.generateSelectedTiles();
        });

        // Refresh game tiles when source toggles change
        document.getElementById('include-base-game').addEventListener('change', () => {
            this.updateGameTilesLayer();
        });

        document.getElementById('include-mods').addEventListener('change', () => {
            this.updateGameTilesLayer();
        });

        // Update grid when map is moved or zoomed
        this.map.on('moveend', () => this.updateGridLayer());
    },

    async loadMaps() {
        try {
            const response = await fetch('/api/maps');
            const data = await response.json();

            const selector = document.getElementById('map-selector');
            selector.innerHTML = '<option value="">Select a map...</option>';

            data.maps.forEach(map => {
                const option = document.createElement('option');
                option.value = map.name;
                option.textContent = `${map.name} (${map.tileCount} tiles)`;
                selector.appendChild(option);
            });
        } catch (error) {
            console.error('Failed to load maps:', error);
            this.showStatus('Failed to load maps', 'error');
        }
    },

    async loadMap(mapName) {
        if (!mapName) {
            this.currentMap = null;
            document.getElementById('map-info').style.display = 'none';
            this.clearGridLayer();
            if (this.gameTilesLayer) {
                this.map.removeLayer(this.gameTilesLayer);
                this.gameTilesLayer = null;
            }
            if (this.layerControl) {
                this.map.removeControl(this.layerControl);
                this.layerControl = null;
            }
            return;
        }

        try {
            const response = await fetch(`/api/maps/${mapName}`);
            if (!response.ok) {
                throw new Error('Map not found');
            }

            this.currentMap = await response.json();
            this.currentMap.name = mapName;

            // Update UI
            document.getElementById('map-info').style.display = 'block';
            document.getElementById('map-details').innerHTML = `
                <div><strong>Origin:</strong> ${this.currentMap.origin.latitude.toFixed(6)}, ${this.currentMap.origin.longitude.toFixed(6)}</div>
                <div><strong>Tile Dimension:</strong> ${this.currentMap.tileDimension}m</div>
                <div><strong>Total Tiles:</strong> ${this.currentMap.tiles.length}</div>
            `;

            // Center map on origin
            this.map.setView([this.currentMap.origin.latitude, this.currentMap.origin.longitude], 14);

            // Clear tile rectangles so they'll be redrawn with fresh styling
            this.cleanupAllGridTiles();

            // Build a Set of all discovered tiles for quick lookup
            this.generatedTiles.clear();
            this.currentMap.tiles.forEach(tile => {
                this.generatedTiles.add(`${tile.x},${tile.y}`);
            });

            // Create game tiles layer as a standard tile layer
            if (this.gameTilesLayer) {
                this.map.removeLayer(this.gameTilesLayer);
            }

            const includeBaseGame = document.getElementById('include-base-game').checked;
            const includeMods = document.getElementById('include-mods').checked;

            this.gameTilesLayer = L.tileLayer(`/api/game-tile-pyramid/${mapName}/{z}/{x}/{y}?includeBaseGame=${includeBaseGame}&includeMods=${includeMods}`, {
                maxZoom: 18,
                opacity: 0.7,
                attribution: 'Game Tiles'
            }).addTo(this.map);

            // Update layer control
            if (this.layerControl) {
                this.map.removeControl(this.layerControl);
            }

            const overlays = {
                'Generated Tiles': this.gameTilesLayer,
                'Tile Grid': this.gridLayer
            };
            this.layerControl = L.control.layers(this.baseLayers, overlays, { collapsed: false }).addTo(this.map);

            // Redraw grid layer
            this.updateGridLayer();

        } catch (error) {
            console.error('Failed to load map:', error);
            this.showStatus(`Failed to load map: ${error.message}`, 'error');
        }
    },

    updateGridLayer() {
        if (!this.map.hasLayer(this.gridLayer) || !this.currentMap) {
            return;
        }

        if (this.map.getZoom() < this.MIN_GRID_ZOOM) {
            this.cleanupAllGridTiles();
            return;
        }

        const origin = this.currentMap.origin;
        const tileDim = this.currentMap.tileDimension;

        const bounds = this.map.getBounds();
        const swTile = CoordinateConverter.latLngToGameTile(
            bounds.getSouth(), bounds.getWest(),
            origin.latitude, origin.longitude, tileDim
        );
        const neTile = CoordinateConverter.latLngToGameTile(
            bounds.getNorth(), bounds.getEast(),
            origin.latitude, origin.longitude, tileDim
        );

        const padMinX = swTile.x - this.TILE_BUFFER;
        const padMaxX = neTile.x + this.TILE_BUFFER;
        const padMinY = swTile.y - this.TILE_BUFFER;
        const padMaxY = neTile.y + this.TILE_BUFFER;

        let tileCount = (padMaxX - padMinX + 1) * (padMaxY - padMinY + 1);

        let minX = padMinX;
        let maxX = padMaxX;
        let minY = padMinY;
        let maxY = padMaxY;

        if (tileCount > this.MAX_GRID_TILES) {
            const centerTile = CoordinateConverter.latLngToGameTile(
                this.map.getCenter().lat, this.map.getCenter().lng,
                origin.latitude, origin.longitude, tileDim
            );
            const halfCount = Math.floor(Math.sqrt(this.MAX_GRID_TILES) / 2);
            minX = centerTile.x - halfCount;
            maxX = centerTile.x + halfCount;
            minY = centerTile.y - halfCount;
            maxY = centerTile.y + halfCount;
        }

        const visibleTiles = new Set();
        for (let y = minY; y <= maxY; y++) {
            for (let x = minX; x <= maxX; x++) {
                const tileKey = `${x},${y}`;
                visibleTiles.add(tileKey);

                if (!this.tileRectangles.has(tileKey)) {
                    this.drawGridTile(x, y);
                }
            }
        }

        for (const [tileKey, { rectangle, label }] of this.tileRectangles) {
            if (!visibleTiles.has(tileKey)) {
                rectangle.remove();
                label.remove();
                this.tileRectangles.delete(tileKey);
            }
        }
    },

    drawGridTile(x, y) {
        const bounds = CoordinateConverter.gameTileToBounds(
            x, y,
            this.currentMap.origin.latitude,
            this.currentMap.origin.longitude,
            this.currentMap.tileDimension
        );

        const latLngBounds = [
            [bounds.minLat, bounds.minLng],
            [bounds.maxLat, bounds.maxLng]
        ];

        const tileKey = `${x},${y}`;
        const isGenerated = this.generatedTiles.has(tileKey);
        const isSelected = this.selectedTiles.has(tileKey);

        let className = 'tile-not-generated';
        if (isGenerated) className = 'tile-generated';
        if (isSelected) className = 'tile-selected';

        const rectangle = L.rectangle(latLngBounds, {
            color: isSelected ? '#f39c12' : (isGenerated ? '#2ecc71' : '#e74c3c'),
            weight: isSelected ? 3 : 2,
            fillOpacity: isSelected ? 0.2 : 0,
            dashArray: isGenerated ? null : '5, 5',
            className: className
        });

        rectangle.on('click', (e) => {
            L.DomEvent.stopPropagation(e);
            this.toggleTileSelection(x, y);
        });

        rectangle.addTo(this.gridLayer);

        const label = L.marker([bounds.centerLat, bounds.centerLng], {
            icon: L.divIcon({
                className: 'tile-label',
                html: `${x},${y}`,
                iconSize: [40, 20],
                iconAnchor: [20, 10]
            }),
            interactive: false
        }).addTo(this.gridLayer);

        this.tileRectangles.set(tileKey, { rectangle, label });
    },

    toggleTileSelection(x, y) {
        if (!this.currentMap) return;

        const tileKey = `${x},${y}`;
        if (this.selectedTiles.has(tileKey)) {
            this.selectedTiles.delete(tileKey);
        } else {
            this.selectedTiles.add(tileKey);
        }

        this.updateGridLayer();
        this.updateSelectionInfo();
    },

    updateSelectionInfo() {
        const count = this.selectedTiles.size;
        const info = document.getElementById('selection-info');
        const generateBtn = document.getElementById('generate-btn');

        if (count === 0) {
            info.textContent = 'Click tiles to select them for generation';
            generateBtn.disabled = true;
        } else {
            // Show condensed tile list
            if (count <= 5) {
                const tiles = Array.from(this.selectedTiles).map(key => {
                    const [x, y] = key.split(',');
                    return `(${x},${y})`;
                }).join(', ');
                info.textContent = `Selected ${count} tile(s): ${tiles}`;
            } else {
                // Show first 3 and last 2 tiles with ellipsis
                const tileArray = Array.from(this.selectedTiles);
                const first3 = tileArray.slice(0, 3).map(key => {
                    const [x, y] = key.split(',');
                    return `(${x},${y})`;
                });
                const last2 = tileArray.slice(-2).map(key => {
                    const [x, y] = key.split(',');
                    return `(${x},${y})`;
                });
                info.textContent = `Selected ${count} tile(s): ${first3.join(', ')}, ... ${last2.join(', ')}`;
            }
            generateBtn.disabled = false;
        }
    },

    async generateSelectedTiles() {
        if (this.selectedTiles.size === 0 || !this.currentMap || this.isGenerating) return;

        const zoom = parseInt(document.getElementById('zoom-level').value);
        const statusDiv = document.getElementById('generation-status');
        const progressDiv = document.getElementById('generation-progress');
        const progressFill = document.getElementById('progress-fill');
        const progressText = document.getElementById('progress-text');
        const generateBtn = document.getElementById('generate-btn');
        const selectionInfo = document.getElementById('selection-info');

        // Lock generation
        this.isGenerating = true;
        generateBtn.disabled = true;
        statusDiv.innerHTML = '<div class="status-info">Generating tiles...</div>';
        progressDiv.style.display = 'block';
        progressFill.style.width = '0%';
        progressText.textContent = '0 / 0 tiles (0%)';

        const tilesToGenerate = Array.from(this.selectedTiles);
        let successCount = 0;
        let errorCount = 0;
        let completedCount = 0;

        // Parallel generation with concurrency limit
        const concurrencyLimit = 20;

        const generateTile = async (tileKey) => {
            const [x, y] = tileKey.split(',').map(Number);

            try {
                const response = await fetch(`/api/maps/${this.currentMap.name}/generate`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ x, y, zoom })
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }

                const result = await response.json();
                this.generatedTiles.add(tileKey);
                successCount++;
                return { success: true, x, y, result };

            } catch (error) {
                console.error(`Failed to generate tile (${x}, ${y}):`, error);
                errorCount++;
                return { success: false, x, y, error };
            }
        };

        try {
            // Process tiles in batches
            for (let i = 0; i < tilesToGenerate.length; i += concurrencyLimit) {
                const batch = tilesToGenerate.slice(i, i + concurrencyLimit);

                // Generate batch in parallel
                const results = await Promise.all(batch.map(generateTile));

                completedCount += batch.length;

                // Update progress
                const percent = (completedCount / tilesToGenerate.length * 100).toFixed(1);
                progressFill.style.width = `${percent}%`;
                progressText.textContent = `${completedCount} / ${tilesToGenerate.length} tiles (${percent}%)`;

                // Show status of last completed tile in batch
                const lastResult = results[results.length - 1];
                if (lastResult.success) {
                    statusDiv.innerHTML = `<div class="status-success">Generated (${lastResult.x}, ${lastResult.y}): ${lastResult.result.min.toFixed(1)}m - ${lastResult.result.max.toFixed(1)}m</div>`;
                } else {
                    statusDiv.innerHTML = `<div class="status-error">Failed (${lastResult.x}, ${lastResult.y}): ${lastResult.error.message}</div>`;
                }
            }

            // Clear selection
            this.selectedTiles.clear();

            // Update UI
            this.updateGridLayer();
            this.updateGameTilesLayer();

            try {
                await this.loadMap(this.currentMap.name);
            } catch (e) {
                console.error('Failed to reload map:', e);
            }

        } finally {
            // Always unlock generation
            this.isGenerating = false;
            generateBtn.disabled = false;
            progressDiv.style.display = 'none';
            selectionInfo.textContent = 'Click tiles to select them for generation';
            statusDiv.innerHTML = `<div class="status-${errorCount > 0 ? 'error' : 'success'}">
                Generated ${successCount} tile(s)${errorCount > 0 ? `, ${errorCount} failed` : ''}
            </div>`;
        }
    },

    onRegionDrawn(e) {
        if (!this.currentMap) {
            alert('Please select a map first');
            return;
        }

        const layer = e.layer;
        this.drawnItems.addLayer(layer);

        // Get the geographic bounds of the drawn shape
        const bounds = layer.getBounds();

        // Find all tiles within this region
        const tilesInRegion = this.getTilesInBounds(bounds);

        // Filter out tiles that already exist
        const missingTiles = tilesInRegion.filter(tileKey => !this.generatedTiles.has(tileKey));

        if (missingTiles.length === 0) {
            alert('All tiles in this region have already been generated');
            this.drawnItems.removeLayer(layer);
            return;
        }

        // Add missing tiles to selection
        missingTiles.forEach(tileKey => this.selectedTiles.add(tileKey));

        // Update UI
        this.updateGridLayer();
        this.updateSelectionInfo();

        // Remove the drawn layer (we've converted it to tile selections)
        this.drawnItems.removeLayer(layer);

        // Show confirmation
        alert(`Selected ${missingTiles.length} missing tile(s) in region`);
    },

    getTilesInBounds(bounds) {
        if (!this.currentMap) return [];

        const origin = this.currentMap.origin;
        const tileDim = this.currentMap.tileDimension;

        // Convert bounds corners to game tile coordinates
        const swTile = CoordinateConverter.latLngToGameTile(
            bounds.getSouth(), bounds.getWest(),
            origin.latitude, origin.longitude, tileDim
        );
        const neTile = CoordinateConverter.latLngToGameTile(
            bounds.getNorth(), bounds.getEast(),
            origin.latitude, origin.longitude, tileDim
        );

        // Generate list of all tiles in the bounding box
        const tiles = [];
        for (let y = swTile.y; y <= neTile.y; y++) {
            for (let x = swTile.x; x <= neTile.x; x++) {
                tiles.push(`${x},${y}`);
            }
        }

        return tiles;
    },

    updateGameTilesLayer() {
        this.clearGameTilesLayer();

        if (!this.currentMap || !this.map.hasLayer(this.gameTilesLayer)) {
            return;
        }

        // Get checkbox states
        const includeBaseGame = document.getElementById('include-base-game').checked;
        const includeMods = document.getElementById('include-mods').checked;

        // Determine visible tile range (same logic as grid)
        const origin = this.currentMap.origin;
        const tileDim = this.currentMap.tileDimension;
        const bounds = this.map.getBounds();

        const swTile = CoordinateConverter.latLngToGameTile(
            bounds.getSouth(), bounds.getWest(),
            origin.latitude, origin.longitude, tileDim
        );
        const neTile = CoordinateConverter.latLngToGameTile(
            bounds.getNorth(), bounds.getEast(),
            origin.latitude, origin.longitude, tileDim
        );

        // Add some padding
        const minX = swTile.x - 2;
        const maxX = neTile.x + 2;
        const minY = swTile.y - 2;
        const maxY = neTile.y + 2;

        // Only render visible generated tiles
        for (let y = minY; y <= maxY; y++) {
            for (let x = minX; x <= maxX; x++) {
                const tileKey = `${x},${y}`;
                if (!this.generatedTiles.has(tileKey)) {
                    continue; // Skip if tile doesn't exist
                }

                const tileBounds = CoordinateConverter.gameTileToBounds(
                    x, y,
                    this.currentMap.origin.latitude,
                    this.currentMap.origin.longitude,
                    this.currentMap.tileDimension
                );

                const latLngBounds = [
                    [tileBounds.minLat, tileBounds.minLng],
                    [tileBounds.maxLat, tileBounds.maxLng]
                ];

                const imageUrl = `/api/game-tiles/${this.currentMap.name}/${x}/${y}?includeBaseGame=${includeBaseGame}&includeMods=${includeMods}`;

                L.imageOverlay(imageUrl, latLngBounds, {
                    opacity: 0.7,
                    interactive: false
                }).addTo(this.gameTilesLayer);
            }
        }
    },

    clearGridLayer() {
        this.gridLayer.clearLayers();
        this.tileRectangles.clear();
    },

    cleanupAllGridTiles() {
        for (const { rectangle, label } of this.tileRectangles.values()) {
            rectangle.remove();
            label.remove();
        }
        this.tileRectangles.clear();
    },

    clearGameTilesLayer() {
        this.gameTilesLayer.clearLayers();
    },

    onMapClick(e) {
        if (!this.currentMap) return;

        const gameTile = CoordinateConverter.latLngToGameTile(
            e.latlng.lat, e.latlng.lng,
            this.currentMap.origin.latitude,
            this.currentMap.origin.longitude,
            this.currentMap.tileDimension
        );

        this.toggleTileSelection(gameTile.x, gameTile.y);
    },

    showNewMapModal() {
        document.getElementById('new-map-modal').classList.add('show');

        const latInput = document.getElementById('new-map-lat');
        const lngInput = document.getElementById('new-map-lng');

        if (this.currentMap) {
            const center = this.map.getCenter();
            latInput.value = center.lat.toFixed(6);
            lngInput.value = center.lng.toFixed(6);
        } else {
            latInput.value = '';
            lngInput.value = '';
        }
    },

    hideNewMapModal() {
        document.getElementById('new-map-modal').classList.remove('show');
        document.getElementById('new-map-form').reset();
    },

    async createNewMap() {
        const name = document.getElementById('new-map-name').value;
        const latitude = parseFloat(document.getElementById('new-map-lat').value);
        const longitude = parseFloat(document.getElementById('new-map-lng').value);
        const tileDimension = parseFloat(document.getElementById('new-map-dim').value);

        try {
            const response = await fetch('/api/maps', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, latitude, longitude, tileDimension })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || 'Failed to create map');
            }

            this.hideNewMapModal();
            await this.loadMaps();

            // Select the new map
            document.getElementById('map-selector').value = name;
            await this.loadMap(name);

            this.showStatus(`Map "${name}" created successfully`, 'success');
        } catch (error) {
            console.error('Failed to create map:', error);
            this.showStatus(`Failed to create map: ${error.message}`, 'error');
        }
    },

    async loadCacheStats() {
        try {
            const response = await fetch('/api/cache/stats');
            const data = await response.json();

            const statsDiv = document.getElementById('cache-stats');
            if (data.zoomLevels.length === 0) {
                statsDiv.innerHTML = '<div>No cached tiles</div>';
            } else {
                const lines = data.zoomLevels.map(z =>
                    `<div>Zoom ${z.zoom}: ${z.tileCount} tiles</div>`
                ).join('');
                statsDiv.innerHTML = lines;
            }
        } catch (error) {
            console.error('Failed to load cache stats:', error);
        }
    },

    showStatus(message, type) {
        const statusDiv = document.getElementById('generation-status');
        statusDiv.innerHTML = `<div class="status-${type}">${message}</div>`;
    }
};

// Initialize app when page loads
document.addEventListener('DOMContentLoaded', () => {
    App.init();
});
