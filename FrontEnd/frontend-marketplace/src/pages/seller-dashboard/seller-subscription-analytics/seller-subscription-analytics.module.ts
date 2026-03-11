import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { SellerSubscriptionAnalyticsComponent } from './seller-subscription-analytics.component';

@NgModule({
  declarations: [
    SellerSubscriptionAnalyticsComponent
  ],
  imports: [
    CommonModule,
    HttpClientModule
  ],
  exports: [
    SellerSubscriptionAnalyticsComponent
  ]
})
export class SellerSubscriptionAnalyticsModule { }
