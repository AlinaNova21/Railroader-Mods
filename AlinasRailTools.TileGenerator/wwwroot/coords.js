/**
 * Coordinate conversion utilities
 * Must match C# implementation in AlinasRailTools.Shared/Heightmap/Coordinates.cs exactly
 */

const CoordinateConverter = {
    METERS_PER_DEGREE_LAT: 111111.0,
    DEG2RAD: Math.PI / 180.0,

    /**
     * Calculate meters per degree longitude at a given latitude
     */
    metersPerDegreeLon(latitude) {
        return Math.cos(latitude * this.DEG2RAD) * this.METERS_PER_DEGREE_LAT;
    },

    /**
     * Convert lat/lng to MapBox tile coordinates
     * Matches game's LatLongToTile implementation exactly (includes +0.5 offset)
     */
    latLngToMapBoxTile(lat, lng, zoom) {
        // Game's LatLongToMercat - modifies x and y
        let x = lng;
        let y = lat;

        const sinLat = Math.sin(y * this.DEG2RAD);
        x = (x + 180.0) / 360.0;
        y = 0.5 - Math.log((1.0 + sinLat) / (1.0 - sinLat)) / 12.566370614359172;

        // Game's LatLongToTile with +0.5 offset and clamping
        const n = 256 << zoom;
        const tx = this.clamp(x * n + 0.5, 0, n - 1) / 256.0;
        const ty = this.clamp(y * n + 0.5, 0, n - 1) / 256.0;

        return { x: tx, y: ty, zoom: zoom };
    },

    /**
     * Convert MapBox tile coordinates to lat/lng
     */
    mapBoxTileToLatLng(tx, ty, zoom) {
        // Scale back to mercator coordinates
        const n = 1 << zoom;
        const x = tx / n;
        const y = ty / n;

        // Mercator to lat/lng
        const lng = x * 360.0 - 180.0;
        const latRad = Math.atan(Math.sinh(Math.PI * (1 - 2 * y)));
        const lat = latRad / this.DEG2RAD;

        return { lat, lng };
    },

    /**
     * Convert meters offset from origin to lat/lng
     * Matches game's LatLng.AddingMeters
     */
    addMetersToLatLng(originLat, originLng, northMeters, eastMeters) {
        // Calculate latitude first
        const lat = originLat + (northMeters / this.METERS_PER_DEGREE_LAT);

        // Then calculate longitude using the NEW latitude (not origin)
        const lng = originLng + (eastMeters / this.metersPerDegreeLon(lat));

        return { lat, lng };
    },

    /**
     * Convert game tile coordinates to geographic bounds
     * Matches game's TilePositionToLatLng
     */
    gameTileToBounds(tileX, tileY, originLat, originLng, tileDimension) {
        // min = SW corner
        const minOffsetY = tileY * tileDimension;
        const minOffsetX = tileX * tileDimension;

        const minLat = originLat + (minOffsetY / this.METERS_PER_DEGREE_LAT);
        const minLng = originLng + (minOffsetX / this.metersPerDegreeLon(minLat));

        // max = NE corner
        const maxOffsetY = (tileY + 1) * tileDimension;
        const maxOffsetX = (tileX + 1) * tileDimension;

        const maxLat = originLat + (maxOffsetY / this.METERS_PER_DEGREE_LAT);
        const maxLng = originLng + (maxOffsetX / this.metersPerDegreeLon(maxLat));

        return {
            minLat,
            minLng,
            maxLat,
            maxLng,
            centerLat: (minLat + maxLat) / 2,
            centerLng: (minLng + maxLng) / 2
        };
    },

    /**
     * Convert lat/lng to game tile coordinates
     */
    latLngToGameTile(lat, lng, originLat, originLng, tileDimension) {
        // Calculate meters from origin
        const northMeters = (lat - originLat) * this.METERS_PER_DEGREE_LAT;

        // Use the target latitude for longitude calculation (approximate)
        const eastMeters = (lng - originLng) * this.metersPerDegreeLon(lat);

        // Convert to tile indices
        const tileX = Math.floor(eastMeters / tileDimension);
        const tileY = Math.floor(northMeters / tileDimension);

        return { x: tileX, y: tileY };
    },

    clamp(value, min, max) {
        return Math.max(min, Math.min(max, value));
    }
};
