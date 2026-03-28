import { Injectable } from '@angular/core';

export interface GeoPositionSnapshot {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
}

@Injectable({ providedIn: 'root' })
export class GeolocationService {
  async getCurrentPosition(): Promise<GeoPositionSnapshot> {
    if (!('geolocation' in navigator)) {
      throw new Error('La géolocalisation n\'est pas disponible sur cet appareil.');
    }

    return new Promise<GeoPositionSnapshot>((resolve, reject) => {
      navigator.geolocation.getCurrentPosition(
        position => {
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracyMeters: position.coords.accuracy
          });
        },
        error => reject(new Error(error.message)),
        {
          enableHighAccuracy: true,
          maximumAge: 0,
          timeout: 15000
        }
      );
    });
  }
}
